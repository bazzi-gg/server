using System;
using Bazzigg.Database.Context;
using Bazzigg.Database.Model.Match;

using Kartrider.Api;
using Kartrider.Api.Endpoints.MatchEndpoint.Models;
using Kartrider.Metadata;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Server.Extensions;
using Server.Models.Response;

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Server.Services;

namespace Server.Controllers
{
    [Authorize]
    [Route("[controller]")]
    [ApiController]
    public class MatchController : Controller
    {
        readonly IKartriderApi _kartriderApi;
        readonly IKartriderMetadata _kartriderMetadata;
        readonly AppDbContext _appDbContext;
        private readonly IPlayerService _playerService;
        public MatchController(IKartriderApi kartriderApi, IKartriderMetadata kartriderMetadata
            , AppDbContext appDbContext,IPlayerService playerService)
        {
            _kartriderApi = kartriderApi;
            _kartriderMetadata = kartriderMetadata;
            _appDbContext = appDbContext;
            _playerService = playerService;
        }
        [HttpGet("detail")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<MatchDetailResponse>> GetMatchDetail([FromQuery] string matchId, [FromQuery] string accessId)
        {
            MatchDetail matchDetail;
            try
            {
                matchDetail = await _kartriderApi.Match.GetMatchDetailAsync(matchId);
            }
            catch (KartriderApiException e) when (e.HttpStatusCode == HttpStatusCode.NotFound)
            {
                return NotFound();
            }

            int meIndex = matchDetail.Players.FindIndex(p => p.AccessId == accessId);

            List<MatchDetailResponse.Player> players = new(matchDetail.Players.Count);

            foreach (Player player in matchDetail.Players)
            {
                bool? hasEmblem = (await _appDbContext.PlayerSummary.FindAsync(player.Nickname))?.RacingMasterEmblem;
                if (!hasEmblem.GetValueOrDefault() && player.License == License.L1)
                {
                    hasEmblem = _playerService.HasRacingMasterEmblem(player.Nickname);
                }
                players.Add(new MatchDetailResponse.Player()
                {
                    FlyingPet = string.IsNullOrEmpty(player.FlyingPet)
                    ? "없음"
                    : _kartriderMetadata[MetadataType.FlyingPet, player.FlyingPet, "알 수 없음"],
                    License = player.License,
                    RacingMasterEmblem = hasEmblem != null && hasEmblem.Value,
                    Nickname = player.Nickname,
                    Rank = player.Rank == 0 ? 99 : player.Rank,
                    Record = player.Record,
                    MyTeam = matchDetail.IsTeamMode && player.TeamType == matchDetail.Players[meIndex].TeamType,
                    Character = _kartriderMetadata[MetadataType.Character, player.Character, "알 수 없음"],
                    Kartbody = _kartriderMetadata[MetadataType.Kart, player.Kartbody, "알 수 없음"],
                    KartbodyHash = player.Kartbody,
                    CharacterHash = player.Character,
                });
            }
            players.Sort((left,right) => left.Rank - right.Rank);
            return Ok(new MatchDetailResponse()
            {
                Index = meIndex,
                Players = players
            });
        }
        [HttpGet("more-matches")]
        [ProducesResponseType(typeof(MoreMatchesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult<IEnumerable<MatchPreview>>> MoreMatches([FromQuery] string accessId, [FromQuery] string startMatchId, [FromQuery] string channel)
        {
            bool isAllChannel = channel.Equals("all"); // 채널 상관없이 모든 매치를 가져올것인지 여부
            string matchType = isAllChannel ? "" : Helpers.ChannelString.ChannelToMatchType(channel);
            if (matchType == null)
            {
                return BadRequest("알 수 없는 채널입니다.");
            }
            const int collectLimit = 15; // 수집해야하는 데이터 수
            List<MatchPreview> collectMatches = new(collectLimit); //리턴할 매치 배열

            var matchesByAccessId = await _kartriderApi.Match.GetMatchesByAccessIdAsync(accessId, null, null, 0, 500, new[] { matchType });

            IEnumerable<MatchInfo> matchInfos = matchesByAccessId.Matches.SelectMany(p => p.Value)
                .OrderByDescending(p => p.StartDateTime);
            // channel이 all이 아닌 경우, 특정 채널만 걸러내기 위함임
            if (!isAllChannel)
            {
                matchInfos = matchInfos.Where(p => p.Channel == channel);
            }
            int startIdx = matchInfos.IndexOf(p => p.MatchId == startMatchId);
            if (startIdx != -1)
            {
                IEnumerable<MatchPreview> result = matchInfos.Skip(startIdx + 1).Select(p => p.ToMatchPreview(_kartriderMetadata, isAllChannel));
                collectMatches.AddRangeAndLimit(result, collectLimit);
            }
            
            // Http Request가 종료된 후 다음에도 호출할 수 있는지 여부
            bool moreMatches = !(matchInfos.Count() - (startIdx + 1 + collectMatches.Count) <= 0) || startIdx == -1;
            return Ok(new MoreMatchesResponse()
            {
                Matches = collectMatches,
                MoreMatches = moreMatches
            });
        }
    }
}

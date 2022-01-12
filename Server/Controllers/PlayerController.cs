using Bazzigg.Database.Context;
using Bazzigg.Database.Entity;
using Bazzigg.Database.Model.Match;

using HtmlAgilityPack;

using Kartrider.Api;
using Kartrider.Api.Endpoints.MatchEndpoint.Models;
using Kartrider.Api.Endpoints.UserEndpoint;
using Kartrider.Metadata;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Server.Extensions;
using Server.Models.Request;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Server.Services;

namespace Server.Controllers
{
    [Authorize]
    [Route("[controller]")]
    [ApiController]
    public class PlayerController : ControllerBase
    {
        private readonly IKartriderApi _kartriderApi;
        private readonly IKartriderMetadata _kartriderMetadata;
        private readonly AppDbContext _appDbContext;
        private readonly IPlayerService _playerService;
        public PlayerController(IKartriderApi kartriderApi,
            IKartriderMetadata kartriderMetadata,
            AppDbContext appDbContext,
            IPlayerService playerService)
        {
            _kartriderApi = kartriderApi;
            _kartriderMetadata = kartriderMetadata;
            _appDbContext = appDbContext;
            _playerService = playerService;
        }
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IAsyncEnumerable<string>))]
        [HttpGet("nicknames")]

        public async IAsyncEnumerable<string> GetNicknames()
        {
            var nicknames = _appDbContext.PlayerSummary.Select(p => p.Nickname.ToLower()).Distinct().AsAsyncEnumerable();
            await foreach (var nickname in nicknames)
            {
                yield return nickname;
            }
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PlayerDetail))]
        [ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(PlayerDetail))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
        public async Task<ActionResult<PlayerDetail>> Get([FromQuery] string nickname, [FromQuery] string channel)
        {
            string accessId;
            try
            {
                User user = await _kartriderApi.User.GetUserByNicknameAsync(nickname);
                accessId = user.AccessId;
            }
            catch (KartriderApiException e) when (e.HttpStatusCode == HttpStatusCode.NotFound || e.HttpStatusCode == HttpStatusCode.BadRequest)
            {
                return NotFound("해당 닉네임을 가진 플레이어는 존재하지 않습니다.");
            }

            PlayerDetail playerDetail = await _appDbContext.PlayerDetail.FindAsync(accessId, channel);
            if (playerDetail == null)
            {
                playerDetail = new PlayerDetail()
                {
                    Nickname = nickname,
                    License = License.Unknown,
                    AccessId = accessId,
                    Channel = channel,
                    RecentTrackRecords = new List<RecentTrackSummary>(),
                    Matches = new List<MatchPreview>(),
                    RecentMatchSummary = new RecentMatchSummary(),
                    LastRenewal = DateTime.MinValue

                };
                return Accepted(playerDetail);
            }
            else
            {
                LoadPlayerDetail(playerDetail);
                return Ok(playerDetail);
            }
        }
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status418ImATeapot)]
        [ProducesDefaultResponseType]
        public async Task<IActionResult> Post([FromBody] PlayerDetailRequest requestPlayerDetail)
        {

            string nickname = requestPlayerDetail.Nickname, channel = requestPlayerDetail.Channel;
            bool isAllChannel = channel.Equals("all");
            string matchType = isAllChannel ? "" : Helpers.ChannelString.ChannelToMatchType(channel);
            if (matchType == null)
            {
                return BadRequest("알 수 없는 채널입니다.");
            }
            bool speedChannel = channel.Contains("speed");
            // 값 검사 끝, 이제 AccessId를 구해야 한다.
            string accessId;
            try
            {
                User user = await _kartriderApi.User.GetUserByNicknameAsync(nickname);
                accessId = user.AccessId;
            }
            catch (KartriderApiException e) when (e.HttpStatusCode == HttpStatusCode.NotFound)
            {
                return NotFound("플레이어가 존재하지 않습니다.");
            }
            // AccessId를 구했음, 갱신 시간 체크
            PlayerDetail playerDetail = await _appDbContext.PlayerDetail.FindAsync(accessId, channel);
            TimeSpan lastRenewalTimeSpan;
            const int refreshLimitTime = 60;
            if (playerDetail != null&& (lastRenewalTimeSpan = DateTime.Now - playerDetail.LastRenewal).TotalSeconds < refreshLimitTime)
            {
#if !DEBUG
                return StatusCode(418, $"{Convert.ToInt32(lastRenewalTimeSpan.TotalSeconds)}초 전에 갱신했습니다. {refreshLimitTime - Convert.ToInt32(lastRenewalTimeSpan.TotalSeconds)}초후에 다시 시도해주세요.");
#endif
            }
            bool needAddEntity = false;
            if (playerDetail == null)
            {
                playerDetail = new PlayerDetail();
                needAddEntity = true;
            }
            else
            {
                LoadPlayerDetail(playerDetail);
            }
            int collectLimit = speedChannel ? 200 : 20; // 수집해야하는 데이터 수
            // 전적 검색 시작
            List<MatchInfo> collectMatches = new(collectLimit);
            for (int i = 0; collectLimit != collectMatches.Count; i++)
            {
                var matchesByAccessId = await _kartriderApi.Match.GetMatchesByAccessIdAsync(accessId, null, null, i * 500, 500,
                        new[] { matchType });
                IEnumerable<MatchInfo> matchInfos = matchesByAccessId.Matches.SelectMany(p => p.Value)
                    .OrderByDescending(p => p.StartDateTime);
                // channel이 all이 아닌 경우, 특정 채널만 걸러내기 위함임
                if (!isAllChannel)
                {
                    matchInfos = matchInfos.Where(p => p.Channel == channel);
                }
                int matchCount = matchInfos.Count(); // 수집한 데이터 개수
                if (i == 0 && matchCount == 0)
                {
                    return NoContent();
                }

                collectMatches.AddRangeAndLimit(matchInfos, collectLimit);
                // matchCount가 200이하면 더 이상 수집할 데이터가 없다는 의미
                if (matchCount < 200)
                {
                    break;
                }
            }
            MatchInfo validMatch = collectMatches.FirstOrDefault(p => p.Player.License != License.Unknown);
            License license = validMatch == null ? License.Unknown : validMatch.Player.License;
            string character = validMatch == null ? collectMatches[0].Player.Character : validMatch.Player.Character;
            playerDetail.Character = character;
            playerDetail.AccessId = accessId;
            playerDetail.Channel = channel;
            playerDetail.Nickname = nickname;
            playerDetail.License = license;
            playerDetail.LastRenewal = DateTime.Now;
            playerDetail.RecentMatchSummary = CreateRecentMatchSummary(collectMatches.Take(20));
            playerDetail.RecentTrackRecords = CreateTrackRecords(speedChannel ? collectMatches : Enumerable.Empty<MatchInfo>());
            playerDetail.Matches = collectMatches.Take(20).Select(p => p.ToMatchPreview(_kartriderMetadata, isAllChannel)).ToList();
            if (!playerDetail.RacingMasterEmblem && license == License.L1)
            {
                playerDetail.RacingMasterEmblem = _playerService.HasRacingMasterEmblem(nickname);
            }
            else if (!playerDetail.RacingMasterEmblem && license == License.PRO)
            {
                playerDetail.RacingMasterEmblem = true;
            }
            if (needAddEntity)
            {
                _appDbContext.PlayerDetail.Add(playerDetail);
            }
            UpdatePlayerSummary(character, license, nickname, playerDetail.RacingMasterEmblem);
            try
            {
                await _appDbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException) // 아주 극단적인 경우가 아니라면 DB에 있는 데이터와 현재 엔티티가 동일함
            {

            }
            return StatusCode(201, playerDetail);
        }

        private void UpdatePlayerSummary(string characterHash, License license, string nickname, bool racingMasterEmblem)
        {
            PlayerSummary playerSummary = _appDbContext.PlayerSummary.Find(nickname);
            if (playerSummary == null)
            {
                playerSummary = new PlayerSummary()
                {
                    Nickname = nickname
                };
                _appDbContext.Entry(playerSummary).State = EntityState.Added;
            }
            playerSummary.CharacterHash = characterHash;
            playerSummary.License = license;
            playerSummary.RacingMasterEmblem = racingMasterEmblem;
        }
        private void LoadPlayerDetail(PlayerDetail playerDetail)
        {
            _appDbContext.Entry(playerDetail).Collection(b => b.Matches).Load();
            _appDbContext.Entry(playerDetail).Collection(b => b.RecentTrackRecords).Load();
        }
        private List<RecentTrackSummary> CreateTrackRecords(IEnumerable<MatchInfo> matchInfos)
        {
            if (matchInfos == Enumerable.Empty<MatchInfo>())
            {
                return new List<RecentTrackSummary>();
            }
            var tracks = matchInfos.GroupBy(p => p.TrackId).Select(p => p.Key);
            List<RecentTrackSummary> playerTrackRecords = new(matchInfos.Count());
            foreach (string trackHash in tracks)
            {
                string trackName = _kartriderMetadata[MetadataType.Track, trackHash, "알 수 없음"];
                int trackPlayCount = matchInfos.Count(p => p.TrackId == trackHash); //해당 트랙을 플레이한 수를 구한다.
                int win = matchInfos.Count(p => p.TrackId == trackHash && p.Player.Win); //승리한 매치 수를 구한다.
                int lose = trackPlayCount - win;
                double winningRate = (double)win / (win + lose) * 100.0;
                IEnumerable<TimeSpan> records = matchInfos.Where(p => p.TrackId == trackHash && !p.Player.Retired && p.Player.Rank != -1 && p.Player.Record != TimeSpan.Zero)
                    .Select(p => p.Player.Record);
                TimeSpan bestTime = TimeSpan.Zero;
                if (records.Any())
                {
                    bestTime = records.Min();
                }
                string channel = matchInfos.ElementAt(0).Channel;
                var record = _appDbContext.TrackRecord.FirstOrDefault(p => p.Channel == channel && p.TrackId == trackHash);
                double top = -1;
                if (record != null && bestTime != TimeSpan.Zero)
                {
                    var recordList = record.Records;
                    int n = recordList.BinarySearch(bestTime.TotalSeconds);
                    n = Math.Abs(n);
                    top = (double)n / recordList.Count * 100;
                    if (top > 100.0)
                    {
                        top = 100;
                    }
                }
                playerTrackRecords.Add(new RecentTrackSummary()
                {
                    BestTime = bestTime,
                    Lose = lose,
                    Top = top,
                    TrackHash = trackHash,
                    Track = trackName,
                    TrackPlayCount = trackPlayCount,
                    Win = win,
                    WinningRate = winningRate
                });
            }
            return playerTrackRecords;

        }
        private RecentMatchSummary CreateRecentMatchSummary(IEnumerable<MatchInfo> matchInfos)
        {
            int matchCount = matchInfos.Count();
            int win = matchInfos.Count(p => p.Player.Win);
            int lose = matchCount - win;
            double winRate = (double)win / (win + lose) * 100;
            string mostUsedKartbody = matchInfos.GroupBy(p => p.Player.Kartbody).OrderByDescending(p => p.Count()).First().Key;
            mostUsedKartbody = _kartriderMetadata[MetadataType.Kart, mostUsedKartbody, "알 수 없음"];
            string mostPlayedTrack = matchInfos.GroupBy(p => p.TrackId).OrderByDescending(p => p.Count()).First().Key;
            mostPlayedTrack = _kartriderMetadata[MetadataType.Track, mostPlayedTrack, "알 수 없음"];
            IEnumerable<MatchInfo> notRetireMatches = matchInfos.Where(p => !p.Player.Retired && p.Player.Rank != 99);
            double averageRank = -1;
            if (notRetireMatches.Any())
            {
                averageRank = notRetireMatches.Select(p => p.Player.Rank).Average();
            }
            return new RecentMatchSummary()
            {
                AverageRank = averageRank,
                Lose = lose,
                MostPlayedTrack = mostPlayedTrack,
                MostUsedKartbody = mostUsedKartbody,
                Win = win,
                WinRate = winRate
            };
        }
    }
}

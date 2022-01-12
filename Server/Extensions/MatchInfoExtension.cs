using Bazzigg.Database.Model.Match;

using Kartrider.Api.Endpoints.MatchEndpoint.Models;
using Kartrider.Metadata;

namespace Server.Extensions
{
    public static class MatchInfoExtension
    {
        public static MatchPreview ToMatchPreview(this MatchInfo matchInfo, IKartriderMetadata kartriderMetadata, bool channelToKorean)
        {
            return new MatchPreview()
            {
                Character = kartriderMetadata[MetadataType.Character, matchInfo.Player.Character, "알 수 없음"],
                CharacterHash = matchInfo.Player.Character,
                EndDateTime = matchInfo.EndDateTime.AddHours(9),
                Kartbody = kartriderMetadata[MetadataType.Kart, matchInfo.Player.Kartbody, "알 수 없음"],
                KartbodyHash = matchInfo.Player.Kartbody,
                MatchId = matchInfo.MatchId,
                Rank = matchInfo.Player.Rank,
                Track = kartriderMetadata[MetadataType.Track, matchInfo.TrackId, "알 수 없음"],
                Win = matchInfo.Player.Win,
                Record = matchInfo.Player.Record,
                Channel = channelToKorean ? kartriderMetadata[ExtendMetadataType.Channel.ToString(), matchInfo.Channel] : null
            };

        }
    }
}

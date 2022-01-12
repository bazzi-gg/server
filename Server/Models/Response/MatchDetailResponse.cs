using Kartrider.Api.Endpoints.MatchEndpoint.Models;

using System;
using System.Collections.Generic;

namespace Server.Models.Response
{
    public class MatchDetailResponse
    {
        public class Player
        {
            public License License { get; set; }
            public bool RacingMasterEmblem { get; set; }
            public string FlyingPet { get; set; }
            public TimeSpan Record { get; set; }
            public bool MyTeam { get; set; }
            public string Nickname { get; set; }
            public int Rank { get; set; }
            public string Kartbody { get; set; }
            public string KartbodyHash { get; set; }
            public string Character { get; set; }
            public string CharacterHash { get; set; }
        }
        public IEnumerable<Player> Players { get; set; }

        public int Index { get; set; }

    }
}

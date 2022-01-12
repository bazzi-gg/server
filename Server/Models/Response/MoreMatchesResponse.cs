using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bazzigg.Database.Model.Match;

namespace Server.Models.Response
{
    public class MoreMatchesResponse
    {
        public bool MoreMatches { get; set; }
        public List<MatchPreview> Matches { get; set; }
    }
}

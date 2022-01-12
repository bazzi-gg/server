using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server
{
    public static class Define
    {
        public static TimeSpan MatchDatetimeSubtractionLimit
        {
            get
            {
                DateTime now = DateTime.Now;
                return now.AddYears(1).AddMonths(1) - now;
            }
        }
    }
}

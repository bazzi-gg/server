
using Bazzigg.Database.Context;
using Bazzigg.Database.Entity;


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.Collections.Generic;

namespace Server.Controllers
{
    [Authorize]
    [Route("[controller]")]
    [ApiController]
    public class AutocompleteController : ControllerBase
    {
        readonly AppDbContext _appDbContext;
        public AutocompleteController(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }
        [HttpGet]
        public async IAsyncEnumerable<PlayerSummary> Get([FromQuery] string keyword)
        {
            var playerSummarys = _appDbContext.PlayerSummary.FromSqlInterpolated($"CALL GetPlayerSummarys({keyword})").AsAsyncEnumerable();
            await foreach (var playerSummary in playerSummarys)
            {
                yield return playerSummary;
            }
        }
    }
}

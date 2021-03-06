
using Bazzigg.Database.Context;
using Bazzigg.Database.Entity;


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

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
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PlayerSummary>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<IEnumerable<PlayerSummary>> Get([FromQuery] string keyword)
        {
            if (string.IsNullOrEmpty(keyword) || 12 < keyword.Length)
            {
                return BadRequest();
            }

            var playerSummarys = _appDbContext.PlayerSummary.FromSqlInterpolated($"CALL GetPlayerSummarys({keyword})")
                .AsEnumerable();
            return Ok(playerSummarys);
        }
    }
}

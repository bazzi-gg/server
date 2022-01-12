using Bazzigg.Database.Context;
using Bazzigg.Database.Entity;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Collections.Generic;

namespace Server.Controllers
{
    [Authorize]
    [Route("[controller]")]
    [ApiController]
    public class InfluencerController : ControllerBase
    {
        readonly AppDbContext _appDbContext;
        public InfluencerController(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }
        [HttpGet]
        public IEnumerable<Influencer> Get()
        {
            return _appDbContext.Influencer;
        }
    }
}

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RIN.WebAPI.DB.SDB;
using RIN.WebAPI.Models.SDB;

namespace RIN.WebAPI.Controllers
{
    [ApiController]
    [Route("DevTestController")]
    public class DevTestController : TmwController
    {
        private readonly IConfiguration Configuration;
        private readonly ILogger        Logger;
        private readonly SDB            Sdb;

        public DevTestController(IConfiguration configuration, ILogger<DevTestController> logger, SDB sdb)
        {
            Configuration = configuration;
            Logger        = logger;
            Sdb           = sdb;
        }

        [HttpGet]
        public async Task<NewCharaterColors> GetNewCharactersColors(int eyeColorId, int skinColorId, int hairColorId)
        {
            var result = await Sdb.GetNewCharactersColors(eyeColorId, skinColorId, hairColorId);
            return result;
        }
    }
}
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RIN.Core.DB.SDB;
using RIN.Core;
using RIN.Core.SDB;
using RIN.Core.DB;

namespace RIN.WebAPI.Controllers
{
#if DEBUG

    [ApiController]
    [Route("DevTestController")]
    public class DevTestController : TmwController
    {
        private readonly IConfiguration Configuration;
        private readonly ILogger        Logger;
        private readonly SDB            Sdb;
        private readonly DB             Db;

        public DevTestController(IConfiguration configuration, ILogger<DevTestController> logger, SDB sdb, DB db)
        {
            Configuration = configuration;
            Logger        = logger;
            Sdb           = sdb;
            Db            = db;
        }

        [HttpGet("GetNewCharactersColors")]
        public async Task<NewCharaterColors> GetNewCharactersColors(int eyeColorId, int skinColorId, int hairColorId)
        {
            var result = await Sdb.GetNewCharactersColors(eyeColorId, skinColorId, hairColorId);
            return result;
        }
        
        [HttpGet("RegisterAccount")]
        public async Task<(long, string)> RegisterAccount(string email, string password, string country, DateTime? birthday, string referralKey, bool emailOpin = false)
        {
            var result = await Db.RegisterNewAccount(email, password, country, birthday ?? DateTime.Now, referralKey, emailOpin);
            return result;
        }
        
        [HttpGet("TestException")]
        public async Task TestException()
        {
            throw new TmwException();
        }
        
        [HttpGet("TestException2")]
        public async Task TestException2()
        {
            throw new TmwException(Error.Codes.ERR_NAME_IN_USE, "Test exception with a custom message");
        }
    }

#endif
}
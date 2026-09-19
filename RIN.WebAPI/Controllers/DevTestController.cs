using Microsoft.AspNetCore.Mvc;
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
        public async Task<object> RegisterAccount(string email, string password, string country, DateTime? birthday, string? referralKey = null, bool emailOpin = false)
        {
            var (accountId, error) = await Db.RegisterNewAccount(email, password, country, birthday ?? DateTime.Now, referralKey!, emailOpin);
            return new { account_id = accountId, error };
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

        [HttpPost("UpdateCharaterAperance")]
        public async Task UpdateCharaterAperance(long charId, CharacterVisuals visuals)
        {
            var result = Db.UpdateCharacterVisuals(charId, visuals);
        }

        [HttpPost("UpdateBattleframeAperance")]
        public async Task UpdateBattleframeAperance(long bfId, PlayerBattleframeVisuals visuals)
        {
            var result = Db.UpdateBattleframeVisuals(bfId, visuals);
        }

        [HttpPost("AddBattleframeLoadout")]
        public async Task AddBattleframeLoadout(long charId, int battleframe_sdb_id, PlayerBattleframeVisuals? visuals)
        {
            visuals = visuals ?? PlayerBattleframeVisuals.CreateDefault();
            var result = Db.CreateBattleframeLoadout(charId, battleframe_sdb_id, visuals);
        }
    }

#endif
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RIN.Core.Common;
using RIN.Core.DB;
using RIN.Core.DB.SDB;
using RIN.WebAPI.Models.Config;
using RIN.WebAPI.Utils;

namespace RIN.WebAPI.Controllers
{
    [ApiController]
    [Route("Clientapi/api/v3")]
    public partial class ClientAPiV3 : TmwController
    {
        private readonly ServerDefaultsSettings ServerDefaults;
        private readonly ILogger<OperatorController> Logger;
        private readonly DB Db;
        private readonly SDB SDB;

        public ClientAPiV3(IOptions<ServerDefaultsSettings> serverDefaults, ILogger<OperatorController> logger, DB db, SDB sdb)
        {
            ServerDefaults = serverDefaults.Value;
            Logger = logger;
            Db = db;
            SDB = sdb;
        }

        [HttpGet("characters/{characterGuid}/garage_slots")]
        [R5SigAuthRequired]
        public async Task<List<GarageSlot>> GarageSlots(long characterGuid)
        {
            var slots = new List<GarageSlot>()
            {
                new GarageSlot()
                {
                    id                = 0,
                    name              = "Crafting Station",
                    character_guid    = characterGuid,
                    garage_type       =  "crafting_station",
                    item_guid         = 0,
                    equipped_slots    = [],
                    limits            = new SlotLimits() { abilities = 4 },
                    decals            = new List<Decal>(),
                    visual_loadout_id = 0,
                    warpaint_id       = 0,
                    warpaintpatterns  = new List<WarpaintPattern>(),
                    visual_overrides  = new List<VisualOverride>(),
                    unlocked          = true,
                    expires_in_secs   = 0
                }
            };

            return slots;
        }

        [HttpPost("trade/products")]
        [R5SigAuthRequired]
        public async Task<List<TradeItem>> TradeProducts()
        {
            var cosmeticsInfos = await SDB.GetOrnamentsInfoList();
            var items = new List<TradeItem>();
            foreach (var info in cosmeticsInfos)
            {
                var tradeItem = new TradeItem
                {
                    duration    = 0,
                    id          = info.id,
                    remote_id   = info.id,
                    name        = info.lang_name,
                    quanity     = 1,
                    remote_type = "ornaments",
                    prices      = new[]
                    {
                        new TradePrice
                        {
                            amount             = 0,
                            currency_remote_id = 0,
                            currency_type      = "redbean",
                            id                 = 171201
                        }
                    },
                    unlock_context = "account"
                };

                items.Add(tradeItem);
            }

            return items;
        }

        [HttpGet("trade/products/garage_slot_perk_respec")]
        [R5SigAuthRequired]
        public async Task<object> GarageSlotPerkRespec()
        {
            var data = "";

            return Content(data, "application/json");
        }
    }
}

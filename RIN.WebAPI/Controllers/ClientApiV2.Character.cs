using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.AspNetCore.Mvc;
using RIN.WebAPI.Models;
using RIN.WebAPI.Models.ClientApi;
using RIN.Core.Common;
using RIN.WebAPI.Utils;
using RIN.Core.ClientApi;
using RIN.Core;

namespace RIN.WebAPI.Controllers
{
    public partial class ClientApiV2
    {
        // Todo: get from db
        [HttpGet("characters/list")]
        [R5SigAuthRequired]
        public async Task<CharacterListResp> ListCharacters()
        {
            var resp = new CharacterListResp
            {
                is_dev           = false,
                rb_balance       = 0,
                name_change_cost = 0,
            };

            var loginResult = await Db.GetLoginData(GetUid()); // temp
            resp.is_dev     = loginResult.is_dev;
            resp.characters = await Db.GetCharactersForAccount(loginResult.account_id);

            var accountMTX        = await Db.GetAccountMTXData(loginResult.account_id);
            resp.rb_balance       = accountMTX.rb_balance;
            resp.name_change_cost = accountMTX.name_change_cost;

            return resp;
        }

        [HttpPost("characters/{characterGuid}/undelete")]
        [R5SigAuthRequired]
        public async Task<object> Undelete(long characterGuid)
        {
            var loginResult = await Db.GetLoginData(GetUid());
            var restore_result = Db.UndeleteCharacterById(loginResult.account_id, characterGuid);

            if (restore_result.Result.code == Error.Codes.SUCCESS)
            {
                return true;
            }
            else
            {
                return ReturnError(restore_result.Result, 404);
            }
        }

        private Character CreateDefaultChar(string name = "Aero")
        {
            var character = new Character
            {
                character_guid    = 42,
                name              = name,
                unique_name       = name,
                is_dev            = true,
                is_active         = true,
                created_at        = DateTime.Now.AddMonths(-2),
                title_id          = 0,
                time_played_secs  = 0,
                needs_name_change = false,
                max_frame_level   = 20,
                frame_sdb_id      = 76335,
                current_level     = 10,
                gender            = 1,
                current_gender    = "female",
                elite_rank        = 95487,
                last_seen_at      = DateTime.Now,
                visuals = new CharacterBattleframeCombinedVisuals()
                {
                    id     = 0,
                    race   = 0,
                    gender = 1,
                    skin_color = new WebIdValueColor()
                    {
                        id = 118969,
                        value = new WebColor()
                        {
                            color = 4294930822
                        }
                    },
                    voice_set = new WebId()
                    {
                        id = 1033
                    },
                    head = new WebId()
                    {
                        id = 10026
                    },
                    eye_color = new WebIdValueColor()
                    {
                        id = 118980,
                        value = new WebColor()
                        {
                            color = 1633685600
                        }
                    },
                    lip_color = new WebIdValueColor()
                    {
                        id = 1,
                        value = new WebColor()
                        {
                            color = 1
                        }
                    },
                    hair_color = new WebIdValueColor()
                    {
                        id = 77193,
                        value = new WebColor()
                        {
                            color = 1917780001
                        }
                    },
                    facial_hair_color = new WebIdValueColor()
                    {
                        id = 77193,
                        value = new WebColor()
                        {
                            color = 1917780001
                        }
                    },
                    head_accessories = new List<WebIdValueColor>()
                    {
                        new WebIdValueColor()
                        {
                            id = 10117,
                            value = new WebColor()
                            {
                                color = 1211031763
                            }
                        }
                    },
                    ornaments = new List<WebId>(),
                    eyes = new WebId()
                    {
                        id = 10001
                    },
                    hair = new WebIdValueColorId()
                    {
                        id = 10113,
                        color = new WebColorId()
                        {
                            id    = 77193,
                            value = 1917780001
                        }
                    },
                    facial_hair = new WebIdValueColorId()
                    {
                        id = 0,
                        color = new WebColorId()
                        {
                            id    = 77187,
                            value = 1518862368
                        }
                    },
                    glider = new WebId()
                    {
                        id = 0
                    },
                    vehicle = new WebId()
                    {
                        id = 0
                    },
                    //decals      = new List<WebId>(),
                    warpaint_id = 143225,
                    warpaint = new List<uint>()
                        {4216738474, 0, 4216717312, 418250752, 1525350400, 4162844703, 4162844703},
                    decalgradients    = new List<int>(),
                    warpaint_patterns = new List<int>(),
                    visual_overrides  = new List<int>()
                },
                gear = new List<GearSlot>()
                {
                },
                expires_in = 0,
                race       = "human",
                migrations = new List<int>()
            };

            return character;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RIN.WebAPI.Models.ClientApi;
using RIN.WebAPI.Models.Common;

namespace RIN.WebAPI.Controllers
{
    public partial class ClientApiV2
    {
        // Todo: get from db
        [HttpGet("characters/list")]
        public async Task<CharacterListResp> ListCharacters()
        {
            var resp = new CharacterListResp
            {
                is_dev           = true,
                rb_balance       = 0,
                name_change_cost = 0,
                characters = new List<Character>
                {
                    CreateDefaultChar()
                }
            };

            return resp;
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
                visuals = new CharacterVisuals()
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
                    decals      = new List<WebId>(),
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
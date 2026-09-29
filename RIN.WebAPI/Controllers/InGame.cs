using Microsoft.AspNetCore.Mvc;
using RIN.WebAPI.Models.InGame;
using RIN.WebAPI.Utils;

namespace RIN.WebAPI.Controllers
{
    [ApiController]
    [Route("ingame")]
    public class InGame : TmwController
    {
        private static readonly SocialStaticData StaticData = new()
        {
            zones =
            [
                new SocialZone { zone_id = 12,   title = "Nothing" },
                new SocialZone { zone_id = 162,  title = "Devil's Tusk" },
                new SocialZone { zone_id = 448,  title = "New Eden" },
                new SocialZone { zone_id = 803,  title = "Agrievan" },
                new SocialZone { zone_id = 805,  title = "Epicenter" },
                new SocialZone { zone_id = 833,  title = "Razor's Edge" },
                new SocialZone { zone_id = 844,  title = "Omnidyne-M Prototype Stadium" },
                new SocialZone { zone_id = 864,  title = "Unearthed" },
                new SocialZone { zone_id = 865,  title = "Abyss" },
                new SocialZone { zone_id = 868,  title = "Cinerarium" },
                new SocialZone { zone_id = 1003, title = "Crash Down" },
                new SocialZone { zone_id = 1007, title = "Vagrant Dawn" },
                new SocialZone { zone_id = 1008, title = "Icebreaker" },
                new SocialZone { zone_id = 1030, title = "Sertao" },
                new SocialZone { zone_id = 1051, title = "Baneclaw Lair" },
                new SocialZone { zone_id = 1069, title = "Miru" },
                new SocialZone { zone_id = 1089, title = "The ARES-Team" },
                new SocialZone { zone_id = 1093, title = "High Tide" },
                new SocialZone { zone_id = 1099, title = "Taken" },
                new SocialZone { zone_id = 1100, title = "Everything Is Shadow" },
                new SocialZone { zone_id = 1101, title = "Trespass" },
                new SocialZone { zone_id = 1102, title = "Razorwind" },
                new SocialZone { zone_id = 1104, title = "Bathsheba" },
                new SocialZone { zone_id = 1106, title = "Off the Grid" },
                new SocialZone { zone_id = 1113, title = "Safe House" },
                new SocialZone { zone_id = 1114, title = "Consequence" },
                new SocialZone { zone_id = 1117, title = "No Exit" },
                new SocialZone { zone_id = 1125, title = "Accord BattleLab" },
                new SocialZone { zone_id = 1134, title = "Catch of the Day" },
                new SocialZone { zone_id = 1147, title = "Team Death Match" },
                new SocialZone { zone_id = 1151, title = "S.O.S." },
                new SocialZone { zone_id = 1154, title = "Accelerate" },
                new SocialZone { zone_id = 1155, title = "Prison Break" },
                new SocialZone { zone_id = 1162, title = "BattleLab The Danger Room" },
                new SocialZone { zone_id = 1163, title = "Holdout: Jericho" },
                new SocialZone { zone_id = 1171, title = "Gatecrasher" },
                new SocialZone { zone_id = 1173, title = "Defense of Dredge" },
                new SocialZone { zone_id = 1181, title = "Mission 22: Homecoming" }
            ]
        };

        // TODO: Implement panels (dashboard, abuse report, ...)
        [HttpGet("panelmanager")]
        public IActionResult PanelManager()
        {
            const string html = """
                <!DOCTYPE html>
                <html>
                <head>
                <meta charset="UTF-8" />
                <title>InGame</title>
                </head>
                <body>
                </body>
                </html>
                """;

            return Content(html, "text/html");
        }

        [HttpGet("api/v1/social/static_data.json")]
        [R5SigAuthRequired]
        public SocialStaticData SocialStaticData()
        {
            return StaticData;
        }
    }
}

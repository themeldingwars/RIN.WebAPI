using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RIN.WebAPI.Models.ClientApi;
using RIN.WebAPI.Utils;
using System.Text.Json.Serialization;

namespace RIN.WebAPI.Controllers
{
    public partial class ClientApiV2
    {
        [HttpGet("zone_settings")]
        [R5SigAuthRequired]
        public async Task<List<ZoneSettingsResp>> ZoneSettings()
        {
            var zones = await Db.GetZoneSettings();

            var zoneSettings = new List<ZoneSettingsResp>(zones.Count());
            foreach (var zone in zones)
            {
                zone.reward_winner = new ZoneReward { };
                zone.reward_loser = new ZoneReward { };
                zone.cert_requirements = await Db.GetZoneCertRequirements(zone.zone_id);
                zone.difficulty_levels = await Db.GetZoneDifficultyLevels(zone.zone_id);

                zoneSettings.Add(zone);
            }

            return zoneSettings;
        }
    }
}
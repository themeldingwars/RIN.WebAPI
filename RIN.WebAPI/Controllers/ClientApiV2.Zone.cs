using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RIN.WebAPI.Models.ClientApi;
using RIN.WebAPI.Utils;
using System.Runtime.ConstrainedExecution;
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
                var certs = Db.GetZoneCertRequirements(zone.zone_id);
                var diffLevels = Db.GetZoneDifficultyLevels(zone.zone_id);
                await Task.WhenAll(certs, diffLevels);

                zone.cert_requirements = certs.Result;
                zone.difficulty_levels = diffLevels.Result;

                zoneSettings.Add(zone);
            }

            return zoneSettings;
        }
    }
}
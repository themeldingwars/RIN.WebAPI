using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.AspNetCore.Mvc;
using RIN.WebAPI.Models;
using RIN.WebAPI.Models.ClientApi;
using RIN.WebAPI.Models.Common;
using RIN.WebAPI.Utils;

namespace RIN.WebAPI.Controllers
{
    public partial class ClientApiV2
    {
        [HttpGet("zone_settings")]
        [R5SigAuthRequired]
        public async Task<object> ZoneSettings()
        {
            using var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            var zones = await Db.GetZoneSettings();

            var zoneSettings = new List<ZoneSettingsResp>(zones.Count());
            foreach (var zone in zones)
            {
                var settings = new ZoneSettingsResp
                {
                    Id = zone.Id,
                    ZoneId = zone.ZoneId,
                    MissionId = zone.MissionId,
                    Gametype = zone.Gametype,
                    InstanceTypePool = zone.InstanceTypePool,
                    IsPreviewZone = zone.IsPreviewZone,
                    DisplayedName = zone.DisplayedName,
                    DisplayedDesc = zone.DisplayedDesc,
                    Description = zone.Description,
                    DisplayedGametype = zone.DisplayedGametype,
                    CertRequired = zone.CertRequired,
                    XpBonus = zone.XpBonus,
                    SortOrder = zone.SortOrder,
                    RotationPriority = zone.RotationPriority,
                    SkipMatchmaking = zone.SkipMatchmaking,
                    QueueingEnabled = zone.QueueingEnabled,
                    TeamCount = zone.TeamCount,
                    MinPlayersPerTeam = zone.MinPlayersPerTeam,
                    MaxPlayersPerTeam = zone.MaxPlayersPerTeam,
                    MinPlayersAcceptPerTeam = zone.MinPlayersAcceptPerTeam,
                    ChallengeEnabled = zone.ChallengeEnabled,
                    ChallengeMinPlayersPerTeam = zone.ChallengeMinPlayersPerTeam,
                    ChallengeMaxPlayersPerTeam = zone.ChallengeMaxPlayersPerTeam,
                    Images = zone.Images,
                    RewardWinner = new ZoneReward { },
                    RewardLoser = new ZoneReward { },
                    CertRequirements = await Db.GetZoneCertRequirements(zone.ZoneId),
                    DifficultyLevels = await Db.GetZoneDifficultyLevels(zone.ZoneId)
                };

                zoneSettings.Add(settings);
            }

            tx.Complete();

            return zoneSettings;
        }
    }
}
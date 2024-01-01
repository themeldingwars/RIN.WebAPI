using System.Security.AccessControl;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using ProtoBuf;
using RIN.WebAPI.Models.ClientApi;
using RIN.WebAPI.Models.Common;
using RIN.WebAPI.Models.DB;
using RIN.WebAPI.Utils;
using static System.Net.Mime.MediaTypeNames;

namespace RIN.WebAPI.DB
{
    public partial class DB
    {
        public async Task<List<ZoneSettingsResp>> GetZoneSettings()
        {
            const string SELECT_SQL = @"SELECT 
                    id,
                    zone_id,
                    mission_id,
                    gametype,
                    instance_type_pool,
                    is_preview_zone,
                    displayed_name,
                    displayed_desc,
                    description,
                    displayed_gametype,
                    cert_required,
                    xp_bonus,
                    sort_order,
                    rotation_priority,
                    skip_matchmaking,
                    queueing_enabled,
                    team_count,
                    min_players_per_team,
                    max_players_per_team,
                    min_players_accept_per_team,
                    challenge_enabled,
                    challenge_min_players_per_team,
                    challenge_max_players_per_team,
                    images
                    FROM webapi.""ZoneSettings""
                    WHERE is_active = True";

            var zone_settings = await DBCall(async conn => conn.Query<DBZoneSettings>(SELECT_SQL));

            var zones = new List<ZoneSettingsResp>(zone_settings.Count());
            foreach (var setting in zone_settings)
            {
                var zoneSettings = new ZoneSettingsResp
                {
                    Id = setting.id,
                    ZoneId = setting.zone_id,
                    MissionId = setting.mission_id,
                    Gametype = setting.gametype,
                    InstanceTypePool = setting.instance_type_pool,
                    IsPreviewZone = setting.is_preview_zone,
                    DisplayedName = setting.displayed_name,
                    DisplayedDesc = setting.displayed_desc,
                    Description = setting.description,
                    DisplayedGametype = setting.displayed_gametype,
                    CertRequired = setting.cert_required,
                    XpBonus = setting.xp_bonus,
                    SortOrder = setting.sort_order,
                    RotationPriority = setting.rotation_priority,
                    SkipMatchmaking = setting.skip_matchmaking,
                    QueueingEnabled = setting.queueing_enabled,
                    TeamCount = setting.team_count,
                    MinPlayersPerTeam = setting.min_players_per_team,
                    MaxPlayersPerTeam = setting.max_players_per_team,
                    MinPlayersAcceptPerTeam = setting.min_players_accept_per_team,
                    ChallengeEnabled = setting.challenge_enabled,
                    ChallengeMinPlayersPerTeam = setting.challenge_min_players_per_team,
                    ChallengeMaxPlayersPerTeam = setting.challenge_max_players_per_team,
                    Images = setting.images
                };

                zones.Add(zoneSettings);
            }

            return zones;
        }

        public async Task<List<ZoneCertRequirements>> GetZoneCertRequirements(uint zone_setting_id)
        {
            const string SELECT_SQL_CERT = @"SELECT 
                    id,
                    cert_id,
                    authorize_position,
                    difficulty_key,
                    presence
                    FROM webapi.""ZoneCertificates""
                    WHERE zone_setting_id = @zone_setting_id";

            var certificates = await DBCall(async conn => conn.Query<DBZoneCertRequirements>(SELECT_SQL_CERT));

            var certs = new List<ZoneCertRequirements>(certificates.Count());
            foreach (var cert in certificates)
            {
                var zoneCertRequirements = new ZoneCertRequirements
                {
                    Id = cert.id,
                    ZoneSettingId = zone_setting_id,
                    CertId = cert.cert_id,
                    AuthorizePosition = cert.authorize_position,
                    DifficultyKey = cert.difficulty_key,
                    Presence = cert.presence
                };

                certs.Add(zoneCertRequirements);
            }

            return certs;
        }

        public async Task<List<ZoneDifficultyLevels>> GetZoneDifficultyLevels(uint zone_setting_id)
        {
            const string SELECT_SQL_DIFFICULTY = @"SELECT 
                    id,
                    difficulty_key,
                    ui_string,
                    display_level,
                    min_level,
                    max_suggested_level,
                    min_players,
                    max_players,
                    min_players_accept,
                    group_min_players,
                    group_max_players,
                    FROM webapi.""ZoneDifficulty""
                    WHERE zone_setting_id = @zone_setting_id";

            var difficulty = await DBCall(async conn => conn.Query<DBZoneDifficultyLevels>(SELECT_SQL_DIFFICULTY));

            var levels = new List<ZoneDifficultyLevels>(difficulty.Count());
            foreach (var level in difficulty)
            {
                var zoneDifficultyLevel = new ZoneDifficultyLevels
                {
                    Id = level.id,
                    ZoneSettingId = zone_setting_id,
                    DifficultyKey = level.difficulty_key,
                    UiString = level.ui_string,
                    DisplayLevel = level.display_level,
                    MinLevel = level.min_level,
                    MaxSuggestedLevel = level.max_suggested_level,
                    MinPlayers = level.min_players,
                    MaxPlayers = level.max_players,
                    MinPlayersAccept = level.min_players_accept,
                    GroupMinPlayers = level.group_min_players,
                    GroupMaxPlayers = level.group_max_players
                };

                levels.Add(zoneDifficultyLevel);
            }

            return levels;
        }
    }
}
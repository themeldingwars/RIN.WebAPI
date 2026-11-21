using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RIN.Core;
using RIN.WebAPI.Models.ClientApi;
using RIN.WebAPI.Utils;

namespace RIN.WebAPI.Controllers
{
    public partial class ClientAPiV3
    {
        [HttpGet("characters/{characterGuid}/army_applications")]
        [R5SigAuthRequired]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<object> GetPersonalArmyApplications(long characterGuid)
        {
            if (characterGuid != GetCid())
            {
                return ReturnError(
                    Error.Codes.ERR_UNKNOWN,
                    "You can only view your own army applications.",
                    StatusCodes.Status403Forbidden
                );
            }

            return await Db.GetPersonalArmyApplications(characterGuid);
        }

        [HttpGet("characters/{characterGuid}/army_invites")]
        [R5SigAuthRequired]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<object> GetPersonalArmyInvites(long characterGuid)
        {
            if (characterGuid != GetCid())
            {
                return ReturnError(
                    Error.Codes.ERR_UNKNOWN,
                    "You can only view your own army invites.",
                    StatusCodes.Status403Forbidden
                );
            }

            return await Db.GetPersonalArmyInvites(characterGuid);
        }

        [HttpGet("characters/{characterGuid}/leaderboards/{leaderboardId}")]
        [R5SigAuthRequired]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<object> GetLeaderboardResult(
            long characterGuid,
            int leaderboardId,
            [FromQuery] bool army_list = false,
            [FromQuery] bool friends_list = false
        )
        {
            return await Db.GetLeaderboardResult(leaderboardId, GetCid());
        }

        [HttpGet("characters/{characterGuid}/garage_slots/{loadoutId}/perks")]
        [R5SigAuthRequired]
        public async Task<object> GetLoadoutPerks(long characterGuid, int loadoutId)
        {
            if (characterGuid != GetCid())
            {
                return ReturnError(Error.Codes.ERR_UNKNOWN, "You can only view your own perks.", StatusCodes.Status403Forbidden);
            }

            var bandwidth = await Db.GetLoadoutPerkBandwidth(characterGuid, loadoutId);
            if (bandwidth == null)
            {
                return ReturnError(Error.Codes.ERR_UNKNOWN, "Loadout not found.", StatusCodes.Status404NotFound);
            }

            var perks = await Db.GetLoadoutPerks(characterGuid, loadoutId);
            return new LoadoutPerksResp { perks = perks, respecs = 0, max_points = bandwidth.Value };
        }

        [HttpPost("characters/{characterGuid}/garage_slots/{loadoutId}/perks")]
        [R5SigAuthRequired]
        public async Task<object> SaveLoadoutPerks(long characterGuid, int loadoutId, [FromBody] JsonElement body)
        {
            if (characterGuid != GetCid())
            {
                return ReturnError(Error.Codes.ERR_UNKNOWN, "You can only change your own perks.", StatusCodes.Status403Forbidden);
            }

            if (!TryReadPerkIds(body, out var perks))
            {
                return ReturnError(Error.Codes.TMW_MSG, "The perk list could not be read.", StatusCodes.Status400BadRequest);
            }

            if (perks.Count > MaxSlottedPerks)
            {
                return ReturnError(Error.Codes.TMW_MSG, $"At most {MaxSlottedPerks} perks can be slotted.", StatusCodes.Status400BadRequest);
            }

            if (perks.Distinct().Count() != perks.Count)
            {
                return ReturnError(Error.Codes.TMW_MSG, "A perk can only be slotted once.", StatusCodes.Status400BadRequest);
            }

            var bandwidth = await Db.GetLoadoutPerkBandwidth(characterGuid, loadoutId);
            if (bandwidth == null)
            {
                return ReturnError(Error.Codes.ERR_UNKNOWN, "Loadout not found.", StatusCodes.Status404NotFound);
            }

            var costs = await SDB.GetPerkModuleCosts([.. perks]);
            var unknown = perks.Where(perk => !costs.ContainsKey(perk)).ToList();
            if (unknown.Count > 0)
            {
                Logger.LogWarning("Character {CharacterGuid} tried to slot {Unknown}, which are not perks, in loadout {LoadoutId}",
                    characterGuid, string.Join(", ", unknown), loadoutId);
                return ReturnError(Error.Codes.TMW_MSG, "That is not a perk.", StatusCodes.Status400BadRequest);
            }

            var total = perks.Sum(perk => costs[perk]);
            if (total > bandwidth.Value)
            {
                return ReturnError(Error.Codes.TMW_MSG, $"These perks cost {total} points, the battleframe has {bandwidth.Value}.", StatusCodes.Status400BadRequest);
            }

            if (!await Db.ReplaceLoadoutPerks(characterGuid, loadoutId, [.. perks]))
            {
                return ReturnError(Error.Codes.ERR_UNKNOWN, "Loadout not found.", StatusCodes.Status404NotFound);
            }

            Logger.LogInformation("Character {CharacterGuid} locked in {Count} perks ({Total}/{Bandwidth} points) on loadout {LoadoutId}",
                characterGuid, perks.Count, total, bandwidth.Value, loadoutId);

            return new LoadoutPerksResp { perks = perks, respecs = 0, max_points = bandwidth.Value };
        }

        private const int MaxSlottedPerks = 10;

        private static bool TryReadPerkIds(JsonElement body, out List<int> perks)
        {
            perks = [];

            if (body.ValueKind != JsonValueKind.Object || !body.TryGetProperty("perks", out var list))
            {
                return false;
            }

            IEnumerable<JsonElement> entries = list.ValueKind switch
            {
                JsonValueKind.Array  => list.EnumerateArray(),
                JsonValueKind.Object => list.EnumerateObject().Select(property => property.Value),
                JsonValueKind.Null   => [],
                _                    => null,
            };

            if (entries == null)
            {
                return false;
            }

            foreach (var entry in entries)
            {
                int id;
                if (entry.ValueKind == JsonValueKind.Number && entry.TryGetInt32(out id)) { }
                else if (entry.ValueKind == JsonValueKind.String && int.TryParse(entry.GetString(), out id)) { }
                else
                {
                    return false;
                }

                if (id <= 0)
                {
                    return false;
                }

                perks.Add(id);
            }

            return true;
        }

        // TODO: Implement, the streak should come from the character's logins
        [HttpGet("characters/{characterGuid}/login_streak")]
        [R5SigAuthRequired]
        public async Task<LoginStreak> GetLoginStreak(long characterGuid)
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

            return new LoginStreak
            {
                character_guid = characterGuid,
                streak         = 1,
                first_login    = today,
                last_updated   = today
            };
        }
    }
}

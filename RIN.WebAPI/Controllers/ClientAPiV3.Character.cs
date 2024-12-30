using Microsoft.AspNetCore.Mvc;
using RIN.Core;
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
    }
}

using Microsoft.AspNetCore.Mvc;
using RIN.WebAPI.Utils;

namespace RIN.WebAPI.Controllers
{
    public partial class ClientAPiV3
    {
        [HttpGet("characters/{characterGuid}/army_applications")]
        [R5SigAuthRequired]
        public async Task<object> GetPersonalArmyApplications(long characterGuid)
        {
            return await Db.GetPersonalArmyApplications(characterGuid);
        }

        [HttpGet("characters/{characterGuid}/army_invites")]
        [R5SigAuthRequired]
        public async Task<object> GetPersonalArmyInvites(long characterGuid)
        {
            return await Db.GetPersonalArmyInvites(characterGuid);
        }
    }
}
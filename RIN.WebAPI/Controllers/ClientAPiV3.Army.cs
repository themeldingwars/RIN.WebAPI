using Microsoft.AspNetCore.Mvc;
using RIN.Core.ClientApi;
using RIN.WebAPI.Models.ClientApi;
using RIN.WebAPI.Utils;

namespace RIN.WebAPI.Controllers
{
    public partial class ClientAPiV3
    {
        [HttpGet("armies")]
        [R5SigAuthRequired]
        public async Task<PageResults<ArmyListItem>> GetArmies(
            [FromQuery] int page = 1,
            [FromQuery] int per_page = 11,
            [FromQuery] string q = "")
        {
            return await Db.GetArmies(page, per_page, q);
        }

        [HttpGet("armies/{armyGuid}")]
        [R5SigAuthRequired]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Army>> GetArmy(long armyGuid)
        {
            var army = await Db.GetArmy(armyGuid);

            return army != null ? Ok(army) : NotFound();
        }

        [HttpGet("armies/{armyGuid}/members")]
        [R5SigAuthRequired]
        public async Task<object> GetArmyMembers(
            long armyGuid,
            [FromQuery] int page = 1,
            [FromQuery] int per_page = 100
        )
        {
            return await Db.GetArmyMembers(armyGuid, page, per_page);
        }

        [HttpPost("armies")]
        [R5SigAuthRequired]
        public async Task<ActionResult<Army>> CreateArmy(CreateArmyReq req)
        {
            var armyGuid = Db.CreateArmy(req.name, req.website, req.description,
                req.is_recruiting, req.playstyle, req.region, req.personality);

            return await GetArmy(armyGuid.Result);
        }

        [HttpPut("armies/{armyGuid}")]
        [R5SigAuthRequired]
        public async Task<object> UpdateArmy(long armyGuid, UpdateArmyReq req)
        {
            await Db.UpdateArmy(armyGuid, req.is_recruiting, req.playstyle, req.personality,
                req.region, req.motd, req.website, req.description, req.login_message);

            return Json(new {});
        }

        [HttpPost("armies/{armyGuid}/invite")]
        [R5SigAuthRequired]
        public async Task<object> InviteToArmy(long armyGuid, InviteToArmyReq req)
        {
            await Db.InviteToArmy(armyGuid, req.character_name, req.message);

            return Json(new {});
        }

        [HttpPut("armies/{armyGuid}/step_down")]
        [R5SigAuthRequired]
        public async Task<object> StepDownAsCommander(long armyGuid, [FromBody] StepDownAsArmyCommanderReq req)
        {
            // todo: demote current player to some lower rank and promote req.character_guid to commander

            return Json(new {});
        }

        [HttpPost("armies/{armyGuid}/apply")]
        [R5SigAuthRequired]
        public async Task<object> ApplyToArmy(long armyGuid, [FromBody] ApplyToArmyReq req)
        {
            await Db.ApplyToArmy(armyGuid, GetCid(), req.message);

            return Json(new {});
        }

        [HttpPut("armies/{armyGuid}/establish")]
        [R5SigAuthRequired]
        public async Task<object> EstablishArmyTag(long armyGuid, [FromBody] EstablishArmyTagReq req)
        {
            await Db.EstablishArmyTag(armyGuid, req.tag);

            return Json(new {});
        }

        [HttpPost("armies/{armyGuid}/ranks")]
        [R5SigAuthRequired]
        public async Task<object> AddArmyRank(long armyGuid, [FromBody] AddArmyRankReq req)
        {
            await Db.AddArmyRank(armyGuid, req.can_promote, req.can_edit, req.is_officer,
                req.name, req.can_invite, req.can_kick, req.position);

            return Json(new { });
        }

        [HttpGet("armies/{armyGuid}/ranks")]
        [R5SigAuthRequired]
        public async Task<object> GetArmyRanks(long armyGuid)
        {
            return await Db.GetArmyRanks(armyGuid);
        }

        [HttpGet("armies/{armyGuid}/members/{characterGuid}/rank")]
        [R5SigAuthRequired]
        public async Task<ArmyRank> GetArmyRankForMember(long armyGuid, long characterGuid)
        {
            return await Db.GetArmyRankForMember(armyGuid, characterGuid);
        }

        [HttpPut("armies/{armyGuid}/members/{characterGuid}/public_note")]
        [R5SigAuthRequired]
        public async Task<object> UpdateArmyMemberPublicNote([FromBody] UpdateArmyPublicNoteReq noteReq, long armyGuid, long characterGuid)
        {
            await Db.UpdateArmyMemberPublicNote(armyGuid, characterGuid, noteReq.public_note);

            return Content(noteReq.public_note, "application/json");
        }

        [HttpGet("armies/{armyGuid}/applications")]
        [R5SigAuthRequired]
        public async Task<object> Applications(long armyGuid)
        {
            return await Db.GetArmyApplications(armyGuid);
        }

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
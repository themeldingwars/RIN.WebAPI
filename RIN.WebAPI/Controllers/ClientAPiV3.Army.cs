using Microsoft.AspNetCore.Mvc;
using RIN.Core;
using RIN.Core.ClientApi;
using RIN.Core.Models.ClientApi;
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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<object> GetArmyMembers(
            long armyGuid,
            [FromQuery] int page = 1,
            [FromQuery] int per_page = 100
        )
        {
            if (!await Db.IsMemberOfArmy(GetCid(), armyGuid))
            {
                return ReturnError(
                    Error.Codes.ERR_UNKNOWN,
                    "You are not a member of this army.",
                    StatusCodes.Status403Forbidden
                );
            }

            return await Db.GetArmyMembers(armyGuid, page, per_page);
        }

        [HttpPost("armies")]
        [R5SigAuthRequired]
        public async Task<ActionResult<Army>> CreateArmy([FromBody] CreateArmyReq req)
        {
            // todo: move all checks to stored procedure
            if (await Db.IsMemberOfArmy(GetCid()))
            {
                return ReturnError(
                    Error.Codes.ERR_UNKNOWN,
                    "You are already a member of an army.",
                    StatusCodes.Status422UnprocessableEntity
                    );
            }

            var armyGuid = Db.CreateArmy(req.name, req.website, req.description,
                req.is_recruiting, req.playstyle, req.region, req.personality);

            // todo copypasted from Sleepwalker
            // // Create ranks (owner + default)
            // var ownerRankId = await Db.ArmyCreateOwnerRank(armyInfo.army_id, armyInfo.army_guid);
            // var defaultRankId = await Db.ArmyAddRank(armyInfo.army_id, armyInfo.army_guid, "Soldier", true);
            //
            // if (ownerRankId < 1 || defaultRankId.code != "SUCCESS")
            //     return new Error() { code = Error.Codes.ERR_UNKNOWN, message = "Unknown Error. Failed to create army ranks." };
            //
            // // Add owner to army and set owner rank
            // var ownerMember = await Db.ArmyAddMember(armyInfo.army_id, armyInfo.army_guid, characterGuid, ownerRankId);
            //
            // if (ownerMember.code != "SUCCESS")
            //     return new Error() { code = Error.Codes.ERR_UNKNOWN, message = "Unknown Error. Failed to add owner to army." };
            //
            // tx.Complete();
            return await GetArmy(armyGuid.Result);
        }

        [HttpPut("armies/{armyGuid}")]
        [R5SigAuthRequired]
        public async Task<object> UpdateArmy(long armyGuid, [FromBody] UpdateArmyReq req)
        {
            if (!await Db.ArmyMemberHasPermission(armyGuid, GetCid(), ArmyPermission.can_edit))
            {
                return ReturnError(
                    Error.Codes.ERR_UNKNOWN,
                    "You do not have permission to edit this army.",
                    StatusCodes.Status403Forbidden
                );
            }

            await Db.UpdateArmy(armyGuid, req.is_recruiting, req.playstyle, req.personality,
                req.region, req.motd, req.website, req.description, req.login_message);

            return NoContent();
        }

        [HttpPost("armies/{armyGuid}/invite")]
        [R5SigAuthRequired]
        public async Task<object> InviteToArmy(long armyGuid, [FromBody] InviteToArmyReq req)
        {
            if (!await Db.ArmyMemberHasPermission(armyGuid, GetCid(), ArmyPermission.can_invite))
            {
                return ReturnError(
                    Error.Codes.ERR_UNKNOWN,
                    "You do not have permission to invite to this army.",
                    StatusCodes.Status403Forbidden
                );
            }

            await Db.InviteToArmy(armyGuid, req.character_name, req.message);

            return NoContent();
        }

        [HttpPut("armies/{armyGuid}/leave")]
        [R5SigAuthRequired]
        public async Task<object> LeaveArmy(long armyGuid)
        {
            await Db.LeaveArmy(armyGuid, GetCid());

            return NoContent();
        }

        [HttpPut("armies/{armyGuid}/step_down")]
        [R5SigAuthRequired]
        public async Task<object> StepDownAsCommander(long armyGuid, [FromBody] StepDownAsArmyCommanderReq req)
        {
            // todo: demote current player to some lower rank and promote req.character_guid to commander

            return NoContent();
        }

        [HttpPost("armies/{armyGuid}/apply")]
        [R5SigAuthRequired]
        public async Task<object> ApplyToArmy(long armyGuid, [FromBody] ApplyToArmyReq req)
        {
            // todo: maybe move everything to stored procedure
            // ERR_ARMY_NOT_RECRUITING = "The army is not currently recruiting.";

            // Other errors to use:
            // ARMY_ERROR, ARMY_ERR_UNKNOWN, ERR_APPLICATION_NOT_FOUND, ARMY_ERR_UNAVAILABLE, ERR_COMMANDER_CAN_NOT_LEAVE,
            // ERR_DUPLICATE_APPLICATION, ERR_DUPLICATE_CHARACTER, ERR_INVALID_NAME, ERR_INVALID_TAG, ERR_INVALID_REGION
            if (await Db.IsMemberOfArmy(GetCid()))
            {
                return ReturnError(
                    Error.Codes.ERR_UNKNOWN,
                    "You are already a member of an army.",
                    StatusCodes.Status422UnprocessableEntity
                );
            }

            await Db.ApplyToArmy(armyGuid, GetCid(), req.message);

            return Json(new {});
        }

        [HttpPut("armies/{armyGuid}/establish")]
        [R5SigAuthRequired]
        public async Task<object> EstablishArmyTag(long armyGuid, [FromBody] EstablishArmyTagReq req)
        {
            if (!await Db.ArmyMemberHasPermission(armyGuid, GetCid(), ArmyPermission.can_edit)
                || await Db.GetArmyMemberCount(armyGuid) < 3
               )
            {
                return ReturnError(
                    Error.Codes.ERR_UNKNOWN,
                    "The army needs to have at least 3 members to establish a tag.",
                    StatusCodes.Status422UnprocessableEntity
                );
            }

            await Db.EstablishArmyTag(armyGuid, req.tag);

            return Ok();
        }

        [HttpPost("armies/{armyGuid}/ranks")]
        [R5SigAuthRequired]
        public async Task<object> AddArmyRank(long armyGuid, [FromBody] AddArmyRankReq req)
        {
            if (!await Db.ArmyMemberHasPermission(armyGuid, GetCid(), ArmyPermission.can_edit))
            {
                return ReturnError(
                    Error.Codes.ERR_UNKNOWN,
                    "You do not have permission to add ranks to this army.",
                    StatusCodes.Status403Forbidden
                );
            }

            await Db.AddArmyRank(armyGuid, req.can_promote, req.can_edit, req.is_officer,
                req.name, req.can_invite, req.can_kick, req.position);

            return NoContent();
        }

        [HttpDelete("armies/{armyGuid}/ranks/{rankId}")]
        [R5SigAuthRequired]
        public async Task<object> RemoveArmyRank(long armyGuid, long rankId)
        {
            if (!await Db.ArmyMemberHasPermission(armyGuid, GetCid(), ArmyPermission.can_edit))
            {
                return ReturnError(
                    Error.Codes.ERR_UNKNOWN,
                    "You do not have permission to remove ranks from this army.",
                    StatusCodes.Status403Forbidden
                );
            }

            await Db.RemoveArmyRank(armyGuid, rankId);

            return NoContent();
        }

        // Client only allows batch update of ranks
        [HttpPut("armies/{armyGuid}/ranks/batch")]
        [R5SigAuthRequired]
        public async Task<object> UpdateArmyRanks(long armyGuid, [FromBody] UpdateArmyRanksReq req)
        {
            if (!await Db.ArmyMemberHasPermission(armyGuid, GetCid(), ArmyPermission.can_edit))
            {
                return ReturnError(
                    Error.Codes.ERR_UNKNOWN,
                    "You do not have permission to edit ranks for this army.",
                    StatusCodes.Status403Forbidden
                );
            }

            var tasks = req.Select(rank =>
                Db.UpdateArmyRank(armyGuid, rank.id, rank.can_promote, rank.can_edit,
                    rank.is_officer, rank.name, rank.can_invite, rank.can_kick)
            ).ToList();

            await Task.WhenAll(tasks);

            return true;
        }

        [HttpPut("armies/{armyGuid}/ranks/batch_order")]
        [R5SigAuthRequired]
        public async Task<object> UpdateArmyRanksOrder(long armyGuid, [FromBody] UpdateArmyRanksOrderReq req)
        {
            if (!await Db.ArmyMemberHasPermission(armyGuid, GetCid(), ArmyPermission.can_edit))
            {
                return ReturnError(
                    Error.Codes.ERR_UNKNOWN,
                    "You do not have permission to edit ranks for this army.",
                    StatusCodes.Status403Forbidden
                );
            }

            await Db.UpdateArmyRanksOrder(armyGuid, req.army_rank_ids);

            return Json(new { });
        }

        [HttpPut("armies/{armyGuid}/members/batch_rank")]
        public async Task<bool> UpdateArmyRanksOfMembers(long armyGuid, [FromBody] UpdateArmyRanksOfMembersReq req)
        {
            var tasks = req.Select(member =>
                Db.UpdateArmyRankForMember(armyGuid, member.character_guid, member.army_rank_id)
            ).ToList();

            await Task.WhenAll(tasks);

            return true;
        }

        [HttpGet("armies/{armyGuid}/ranks")]
        [R5SigAuthRequired]
        public async Task<object> GetArmyRanks(long armyGuid)
        {
            if (!await Db.ArmyMemberHasPermission(armyGuid, GetCid(), ArmyPermission.can_edit))
            {
                return ReturnError(
                    Error.Codes.ERR_UNKNOWN,
                    "You do not have permission to view ranks for this army.",
                    StatusCodes.Status403Forbidden
                );
            }

            return await Db.GetArmyRanks(armyGuid);
        }

        [HttpGet("armies/{armyGuid}/members/{characterGuid}/rank")]
        [R5SigAuthRequired]
        // todo: string characterGuid is temporary workaround, because on login the client sends it as `nil`
        public async Task<object> GetArmyRankForMember(long armyGuid, string characterGuid)
        {
            var isValid = long.TryParse(characterGuid, out var charGuid);

            if (isValid && charGuid != GetCid())
            {
                return ReturnError(
                    Error.Codes.ERR_UNKNOWN,
                    "You can only view your own rank.",
                    StatusCodes.Status403Forbidden
                );
            }

            charGuid = GetCid();

            return await Db.GetArmyRankForMember(armyGuid, charGuid);
        }

        [HttpPut("armies/{armyGuid}/members/{characterGuid}/public_note")]
        [R5SigAuthRequired]
        public async Task<object> UpdateArmyMemberPublicNote([FromBody] UpdateArmyPublicNoteReq noteReq, long armyGuid, long characterGuid)
        {
            if (characterGuid != GetCid())
            {
                return ReturnError(
                    "ERR_UNKNOWN",
                    "You can only update your own public note.",
                    StatusCodes.Status403Forbidden
                );
            }

            await Db.UpdateArmyMemberPublicNote(armyGuid, characterGuid, noteReq.public_note);

            return NoContent();
        }

        [HttpGet("armies/{armyGuid}/applications")]
        [R5SigAuthRequired]
        public async Task<object> GetArmyApplications(long armyGuid)
        {
            if (!await Db.ArmyMemberHasPermission(armyGuid, GetCid(), ArmyPermission.can_invite))
            {
                return ReturnError(
                    Error.Codes.ERR_UNKNOWN,
                    "You do not have permission to view applications for this army.",
                    StatusCodes.Status403Forbidden
                );
            }

            return await Db.GetArmyApplications(armyGuid);
        }

        [HttpPost("armies/{armyGuid}/applications/{applicationId}/approve")]
        [R5SigAuthRequired]
        public async Task<object> ApproveArmyApplication(long armyGuid, long applicationId)
        {
            // await Db.ApproveArmyApplication(armyGuid, applicationId);

            return NoContent();
        }

        [HttpPost("armies/{armyGuid}/applications/{applicationId}/reject")]
        [R5SigAuthRequired]
        public async Task<object> RejectArmyApplication(long armyGuid, long applicationId)
        {
            await Db.RejectArmyApplication(armyGuid, applicationId);

            return NoContent();
        }

        [HttpPut("armies/{armyGuid}/members/batch_kick")]
        [R5SigAuthRequired]
        public async Task<object> KickArmyMembers(long armyGuid, [FromBody] KickArmyMembersReq req)
        {
            if (!await Db.ArmyMemberHasPermission(armyGuid, GetCid(), ArmyPermission.can_kick))
            {
                return ReturnError(
                    Error.Codes.ERR_UNKNOWN,
                    "You do not have permission to kick members from this army.",
                    StatusCodes.Status403Forbidden
                );
            }
            await Db.KickArmyMembers(armyGuid, req.CharacterGuids);

            return NoContent();
        }

        [HttpPut("armies/{armyGuid}/member/{characterGuid}/kick")]
        [R5SigAuthRequired]
        public async Task<object> KickArmyMembers(long armyGuid, long characterGuid)
        {
            if (!await Db.ArmyMemberHasPermission(armyGuid, GetCid(), ArmyPermission.can_kick))
            {
                return ReturnError(
                    Error.Codes.ERR_UNKNOWN,
                    "You do not have permission to kick members from this army.",
                    StatusCodes.Status403Forbidden
                );
            }

            await Db.KickArmyMembers(armyGuid, [characterGuid]);

            return NoContent();
        }

        // [HttpPut("armies/{armyGuid}/disband")]
        // [R5SigAuthRequired]
        // public async Task<object> DisbandArmy(long armyGuid)
        // {
        // }
    }
}

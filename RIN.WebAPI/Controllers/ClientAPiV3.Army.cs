using Microsoft.AspNetCore.Mvc;
using RIN.Core;
using RIN.Core.ClientApi;
using RIN.Core.Models.ClientApi;
using RIN.WebAPI.Models.ClientApi;
using RIN.WebAPI.Utils;

namespace RIN.WebAPI.Controllers
{
    [Produces("application/json")]
    [ProducesErrorResponseType(typeof(Error))]
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
        [ProducesErrorResponseType(typeof(void))]
        public async Task<ActionResult<Army>> GetArmy(long armyGuid)
        {
            var army = await Db.GetArmy(armyGuid);

            return army != null ? Ok(army) : NotFound();
        }

        [HttpPost("armies")]
        [R5SigAuthRequired]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

            var result = await Db.UpdateArmy(armyGuid, req.is_recruiting, req.playstyle, req.personality,
                req.region, req.motd, req.website, req.description, req.login_message);

            return result ? new { } : ReturnError(Error.Codes.ERR_UNKNOWN, "Failed to update army.");
        }

        [HttpPut("armies/{armyGuid}/establish")]
        [R5SigAuthRequired]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<object> EstablishArmyTag(long armyGuid, [FromBody] EstablishArmyTagReq req)
        {
            if (!await Db.ArmyMemberHasPermission(armyGuid, GetCid(), ArmyPermission.can_edit))
            {
                return ReturnError(
                    Error.Codes.ERR_UNKNOWN,
                    "You do not have permission to establish a tag for this army.",
                    StatusCodes.Status403Forbidden
                );
            }

            if (await Db.GetArmyMemberCount(armyGuid) < 3) {
                return ReturnError(
                    Error.Codes.ERR_UNKNOWN,
                    "The army needs to have at least 3 members to establish a tag.",
                    StatusCodes.Status422UnprocessableEntity
                );
            }

            var result = await Db.EstablishArmyTag(armyGuid, req.tag);

            return result ? new { } : ReturnError(Error.Codes.ERR_UNKNOWN, "Failed to establish army tag.");
        }

        [HttpPut("armies/{armyGuid}/disband")]
        [R5SigAuthRequired]
        public async Task<object> DisbandArmy(long armyGuid)
        {
            return new { };
        }

        [HttpPut("armies/{armyGuid}/step_down")]
        [R5SigAuthRequired]
        public async Task<object> StepDownAsCommander(long armyGuid, [FromBody] StepDownAsArmyCommanderReq req)
        {
            // todo: demote current player to some lower rank and promote req.character_guid to commander
            // You also have the option of stepping down from this page as leader of the army.
            //     When you use this option, you will type the name of your successor into the field,
            //     and they will take ownership of the army; you will then be assigned the previous rank of that member.
            return NoContent();
        }

        [HttpPut("armies/{armyGuid}/leave")]
        [R5SigAuthRequired]
        public async Task<object> LeaveArmy(long armyGuid)
        {
            await Db.LeaveArmy(armyGuid, GetCid());

            return NoContent();
        }

        [HttpGet("armies/{armyGuid}/ranks")]
        [R5SigAuthRequired]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

        [HttpPost("armies/{armyGuid}/ranks")]
        [R5SigAuthRequired]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

            var result = await Db.AddArmyRank(armyGuid, req.can_promote, req.can_edit, req.is_officer,
                req.name, req.can_invite, req.can_kick, req.position);

            return result ? new { } : ReturnError(Error.Codes.ERR_UNKNOWN, "Failed to add rank to the army.");
        }

        [HttpDelete("armies/{armyGuid}/ranks/{rankId}")]
        [R5SigAuthRequired]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

            var result = await Db.ReassignMembersAndRemoveArmyRank(armyGuid, rankId);

            return result ? new { } : ReturnError(Error.Codes.ERR_UNKNOWN, "Failed to remove rank from the army.");
        }

        // Client only allows batch update of ranks
        [HttpPut("armies/{armyGuid}/ranks/batch")]
        [R5SigAuthRequired]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

            var initiatorRankPosition = await Db.GetArmyRankPosition(armyGuid, GetCid());

            var tasks = req.Select(rank =>
                Db.UpdateArmyRank(armyGuid, initiatorRankPosition, rank.id, rank.can_promote, rank.can_edit,
                    rank.is_officer, rank.name, rank.can_invite, rank.can_kick)
            ).ToList();

            await Task.WhenAll(tasks);

            var successCount = tasks.Count(t => t.Result);

            var skippedCount = req.Count - successCount;

            if (skippedCount > 0)
            {
                return ReturnError(
                    Error.Codes.ERR_UNKNOWN,
                    $"Updated {successCount} ranks, skipped {skippedCount} ranks of higher position than your rank.",
                    StatusCodes.Status422UnprocessableEntity
                );
            }

            return successCount > 0
                ? new { }
                : ReturnError(Error.Codes.ERR_UNKNOWN, "Failed to update army ranks.");
        }

        [HttpPut("armies/{armyGuid}/ranks/batch_order")]
        [R5SigAuthRequired]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

            var result = await Db.UpdateArmyRanksOrder(armyGuid, req.army_rank_ids);

            return result ? new { } : ReturnError(Error.Codes.ERR_UNKNOWN, "Failed to update ranks order.");
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

        [HttpGet("armies/{armyGuid}/members/{characterGuid}/rank")]
        [R5SigAuthRequired]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

        [HttpPut("armies/{armyGuid}/members/batch_rank")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<object> UpdateArmyRanksOfMembers(long armyGuid, [FromBody] UpdateArmyRanksOfMembersReq req)
        {
            if (!await Db.ArmyMemberHasPermission(armyGuid, GetCid(), ArmyPermission.can_promote))
            {
                return ReturnError(
                    Error.Codes.ERR_UNKNOWN,
                    "You do not have permission to update ranks of members in this army.",
                    StatusCodes.Status403Forbidden
                );
            }

            var result = await Db.UpdateArmyRankForMembers(armyGuid, GetCid(), req.ArmyRankId, req.CharacterGuids);

            return result ? new {} : ReturnError(Error.Codes.ERR_UNKNOWN, "Failed to update ranks of members.");
        }

        [HttpPut("armies/{armyGuid}/members/{characterGuid}/public_note")]
        [R5SigAuthRequired]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<object> UpdateArmyMemberPublicNote(long armyGuid, long characterGuid, [FromBody] UpdateArmyPublicNoteReq noteReq)
        {
            if (characterGuid != GetCid())
            {
                return ReturnError(
                    Error.Codes.ERR_UNKNOWN,
                    "You can only update your own public note.",
                    StatusCodes.Status403Forbidden
                );
            }

            var result = await Db.UpdateArmyMemberPublicNote(armyGuid, characterGuid, noteReq.public_note);

            return result
                ? new { noteReq.public_note }
                : ReturnError(Error.Codes.ERR_UNKNOWN, "Failed to update public note."); ;
        }

        [HttpPut("armies/{armyGuid}/members/batch_kick")]
        [R5SigAuthRequired]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
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

            var kickedMembersCount = await Db.KickArmyMembers(armyGuid, GetCid(), req.CharacterGuids);

            var skippedCount = req.CharacterGuids.Length - kickedMembersCount;

            if (skippedCount > 0)
            {
                return ReturnError(
                    Error.Codes.ERR_UNKNOWN,
                    $"Kicked {kickedMembersCount} members from the army, skipped {skippedCount} members with higher rank than yours.",
                    StatusCodes.Status422UnprocessableEntity
                );
            }

            return kickedMembersCount > 0
                ? new { }
                : ReturnError(Error.Codes.ERR_UNKNOWN, "Failed to kick members from the army.");
        }

        [HttpPut("armies/{armyGuid}/members/{characterGuid}/kick")]
        [R5SigAuthRequired]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<object> KickArmyMember(long armyGuid, long characterGuid)
        {
            return await KickArmyMembers(armyGuid, new KickArmyMembersReq { character_guids = [characterGuid.ToString()] });
        }

        [HttpGet("armies/{armyGuid}/applications")]
        [R5SigAuthRequired]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

            return new { };
        }

        [HttpPost("armies/{armyGuid}/applications/{applicationId}/reject")]
        [R5SigAuthRequired]
        public async Task<object> RejectArmyApplication(long armyGuid, long applicationId)
        {
            await Db.RejectArmyApplication(armyGuid, applicationId);

            return new { };
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

            return new { };
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

            return new { };
        }
    }
}

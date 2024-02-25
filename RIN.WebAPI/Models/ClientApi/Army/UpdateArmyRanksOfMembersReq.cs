using System.ComponentModel.DataAnnotations;

namespace RIN.WebAPI.Models.ClientApi
{
    public class UpdateArmyRanksOfMembersReq : List<UpdateArmyRankOfMember>
    {
    }

    public class UpdateArmyRankOfMember
    {
        [Required] public required long army_rank_id   { get; set; }
        [Required] public required long character_guid { get; set; }
    }
}

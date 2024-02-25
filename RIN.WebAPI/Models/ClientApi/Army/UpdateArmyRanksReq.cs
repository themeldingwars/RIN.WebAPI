using System.ComponentModel.DataAnnotations;

namespace RIN.WebAPI.Models.ClientApi
{
    public class UpdateArmyRanksReq : List<UpdateArmyRank>
    {
    }

    public class UpdateArmyRank
    {
        // updating order of the ranks causes client to also trigger their batch update,
        // but it doesn't send `name` inside each object in that case
        public                     string? name        { get; set; }
        [Required] public required long    id          { get; set; }
        [Required] public required bool    can_edit    { get; set; }
        [Required] public required bool    can_invite  { get; set; }
        [Required] public required bool    can_kick    { get; set; }
        [Required] public required bool    is_officer  { get; set; }
        [Required] public required bool    can_promote { get; set; }
    }
}

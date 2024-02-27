using System.ComponentModel.DataAnnotations;

namespace RIN.WebAPI.Models.ClientApi
{
    public class UpdateArmyRanksOfMembersReq : List<UpdateArmyRankOfMember>
    {
        public long ArmyRankId
        {
            get
            {
                if (this.Any())
                {
                    return this.First().army_rank_id;
                }
                throw new InvalidOperationException("The list of army members is empty.");
            }
        }

        public long[] CharacterGuids
        {
            get
            {
                return this.Select(m => m.character_guid).ToArray();
            }
        }
    }

    public class UpdateArmyRankOfMember
    {
        [Required] public required long army_rank_id   { get; set; }
        [Required] public required long character_guid { get; set; }
    }
}

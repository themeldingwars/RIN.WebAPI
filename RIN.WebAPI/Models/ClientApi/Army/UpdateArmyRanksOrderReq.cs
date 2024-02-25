using System.ComponentModel.DataAnnotations;

namespace RIN.WebAPI.Models.ClientApi;

public class UpdateArmyRanksOrderReq
{
    [Required] public required List<int> army_rank_ids { get; set; }
}

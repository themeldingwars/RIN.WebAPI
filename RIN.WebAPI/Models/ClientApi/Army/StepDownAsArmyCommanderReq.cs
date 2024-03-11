using System.ComponentModel.DataAnnotations;

namespace RIN.WebAPI.Models.ClientApi
{
    public class StepDownAsArmyCommanderReq
    {
        [Required] public required string character_name { get; set; }
    }
}

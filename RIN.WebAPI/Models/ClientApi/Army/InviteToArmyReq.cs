using System.ComponentModel.DataAnnotations;

namespace RIN.WebAPI.Models.ClientApi
{
    public class InviteToArmyReq
    {
        public required            string message        { get; set; } = string.Empty;
        [Required] public required string character_name { get; set; }
    }
}

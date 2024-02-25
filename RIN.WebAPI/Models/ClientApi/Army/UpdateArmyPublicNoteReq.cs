using System.ComponentModel.DataAnnotations;

namespace RIN.WebAPI.Models.ClientApi
{
    public class UpdateArmyPublicNoteReq
    {
        [Required] public required string public_note { get; set; }
    }
}

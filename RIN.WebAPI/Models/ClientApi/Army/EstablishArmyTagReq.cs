using System.ComponentModel.DataAnnotations;

namespace RIN.WebAPI.Models.ClientApi
{
    public class EstablishArmyTagReq
    {
        [StringLength(maximumLength: 6, MinimumLength = 3,
            ErrorMessage = "The name tag must have between {2} and {1} characters.")]
        [RegularExpression("^[-a-zA-Z0-9]*$",
            ErrorMessage = "The tag must contain only letters, numbers and hyphens.")]
        public required string tag { get; set; }
    }
}

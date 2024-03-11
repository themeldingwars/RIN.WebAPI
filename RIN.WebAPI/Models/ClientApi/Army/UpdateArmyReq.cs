using System.ComponentModel.DataAnnotations;

namespace RIN.WebAPI.Models.ClientApi
{
    public class UpdateArmyReq
    {
        [Required]
        public required bool is_recruiting { get; set; }

        [AllowedValues("pve", "pvp", "pvx", "PVE", "PVP", "PVX",
            ErrorMessage = "The playstyle must be one of 'pve', 'pvp' or 'pvx'.")]
        [Required]
        // client sends uppercase in update request, but lowercase in create request
        public required string playstyle { get; set; }

        [AllowedValues("Casual", "Moderate", "Hardcore",
            ErrorMessage = "The personality must be one of 'Casual', 'Moderate' or 'Hardcore'.")]
        [Required]
        public required string personality { get; set; }

        [AllowedValues("NA", "Europe", "AUS/NZ", "China", "All",
            ErrorMessage = "The region must be one of 'NA', 'Europe', 'AUS/NZ', 'China' or 'All'.")]
        [Required]
        public required string region { get; set; }

        public string? motd { get; set; }

        [Url(ErrorMessage = "The website is not a valid URL.")]
        public string? website { get; set; }

        [StringLength(maximumLength: 2048, MinimumLength = 1,
            ErrorMessage = "The description must have between {2} and {1} characters.")]
        public string? description { get; set; }

        public string? login_message { get; set; }
    }
}

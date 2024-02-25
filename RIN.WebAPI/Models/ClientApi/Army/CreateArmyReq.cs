using System.ComponentModel.DataAnnotations;

namespace RIN.WebAPI.Models.ClientApi
{
    public class CreateArmyReq
    {
        private string _name;

        [StringLength(maximumLength: 32, MinimumLength = 10,
            ErrorMessage = "The name must must have between {2} and {1} characters.")]
        [RegularExpression("^[a-zA-Z0-9]*$",
            ErrorMessage = "The name must contain only letters and numbers.")]
        [Required]
        public required string name
        {
            get => _name;
            set => _name = value.Trim();
        }

        [Url(ErrorMessage = "The website is not a valid URL.")]
        public string? website { get; set; }

        [StringLength(maximumLength: 2048, MinimumLength = 1,
            ErrorMessage = "The description must have between {2} and {1} characters.")]
        [Required]
        public required string description { get; set; }

        [Required]
        public required bool is_recruiting { get; set; }

        [AllowedValues("pve", "pvp", "pvx",
            ErrorMessage = "The playstyle must be one of 'pve', 'pvp' or 'pvx'.")]
        [Required]
        public required string playstyle { get; set; }

        [AllowedValues("NA", "Europe", "AUS/NZ", "China", "All",
            ErrorMessage = "The region must be one of 'NA', 'Europe', 'AUS/NZ', 'China' or 'All'.")]
        [Required]
        public required string region { get; set; }

        [AllowedValues("Casual", "Moderate", "Hardcore",
            ErrorMessage = "The personality must be one of 'Casual', 'Moderate' or 'Hardcore'.")]
        [Required]
        public required string personality { get; set; }
    }
}

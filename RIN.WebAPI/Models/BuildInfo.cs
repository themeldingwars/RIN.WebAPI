namespace RIN.WebAPI.Models
{
    public class BuildInfo
    {
        public string build       { get; set; }
        public string environment { get; set; }
        public string region      { get; set; }
        public int    patch_level { get; set; }
    }
}
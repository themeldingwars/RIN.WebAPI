namespace RIN.WebAPI.Models.ClientApi
{
    public class CaisStatus
    {
        public string state      { get; set; }
        public long   duration   { get; set; }
        public long   expires_at { get; set; }
    }
}
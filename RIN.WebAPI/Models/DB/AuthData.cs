namespace RIN.WebAPI.Models.DB
{
    public class AuthData
    {
        public long account_id { get; set; }
        public bool is_dev { get; set; }
        public string secret { get; set; }
    }
}

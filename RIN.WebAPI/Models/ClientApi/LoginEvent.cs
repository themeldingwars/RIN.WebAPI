namespace RIN.WebAPI.Models.ClientApi
{
    public class LoginEvent
    {
        public long   id          { get; set; }
        public string name        { get; set; }
        public string description { get; set; }
        public string color       { get; set; }
        public bool   is_active   { get; set; }
        public string created_at  { get; set; }
        public string updated_at  { get; set; }
    }
}

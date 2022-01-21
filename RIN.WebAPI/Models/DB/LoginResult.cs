namespace RIN.WebAPI.Models.DB
{
    public class LoginResult
    {
        public bool   can_login       { get; set; }
        public long   account_id      { get; set; }
        public bool   is_dev          { get; set; }
        public short  character_limit { get; set; }
        public bool   is_vip          { get; set; }
        public long   vip_expiration  { get; set; }
        public string secret          { get; set; }
        public string error           { get; set; }
        public string error_msg       { get; set; }
    }
}
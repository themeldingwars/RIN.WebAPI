namespace RIN.WebAPI.Models.ClientApi
{
    public class LoginResp
    {
        public long       account_id        { get; set; }
        public bool       can_login         { get; set; }
        public bool       is_dev            { get; set; }
        public bool       steam_auth_prompt { get; set; }
        public bool       skip_precursor    { get; set; }
        public CaisStatus cais_status       { get; set; }
        public int        character_limit   { get; set; }
        public bool       is_vip            { get; set; }
        public long       vip_expiration    { get; set; }
    }
}
using System;

namespace RIN.WebAPI.Models.ClientApi
{
    public class CreateAccountReq
    {
        public string referral_key { get; set; }
        public string email        { get; set; }
        public bool   email_optin  { get; set; }
        public string password     { get; set; }
        public string country      { get; set; }
        public string birthday     { get; set; }
        
        
        public string? steam_session_ticket { get; set; }
        public string? steam_user_id        { get; set; }
        public string? steam_cdkey          { get; set; }
    }
}
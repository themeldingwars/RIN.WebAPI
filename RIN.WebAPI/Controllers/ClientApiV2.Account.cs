using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Web;
using FauFau.Net.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RIN.WebAPI.Models;
using RIN.WebAPI.Models.ClientApi;
using RIN.WebAPI.Models.Config;

namespace RIN.WebAPI.Controllers
{
    public partial class ClientApiV2
    {
        // Todo proper login
        [HttpPost("accounts/login")]
        public async Task<ActionResult<LoginResp>> Login()
        {
            var serverDefaults = Configuration.GetSection(ServerDefaultsSettings.NAME).Get<ServerDefaultsSettings>() ?? new ServerDefaultsSettings();
            ContentResult invalidLoginError = ReturnError(Error.Codes.ERR_INCORRECT_USERPASS, "Login failed, check your username and password");

            //Logger.LogInformation($"Headers: {GetHeadersDev()}");

            var uid         = HttpUtility.UrlDecode(GetRed5Sig().UID.ToString());
            var loginResult = await Db.GetLoginData(uid);
            
            // Email not found
            if (loginResult == null) {
                return invalidLoginError;
            }
            
            var authed      = Auth.Verify(loginResult.secret, GetRed5Sig1Str().AsSpan());

            // Auth failed, password mismatch prob
            if (!authed) {
                return invalidLoginError;
            }

            // Fill out the rest of the login data
            var loginData = new LoginResp
            {
                account_id      = loginResult.account_id,
                can_login       = true,
                is_dev          = loginResult.is_dev,
                character_limit = loginResult.character_limit,
                cais_status = new CaisStatus
                {
                    state      = "disabled",
                    duration   = 0,
                    expires_at = 0
                },
                is_vip = false,
                vip_expiration = -1,

                steam_auth_prompt = false,
                skip_precursor    = false,
            };

            loginData.character_limit = loginData.character_limit != -1 ? loginData.character_limit : serverDefaults.CharaterLimitPerAccount;

            return loginData;
        }

        [HttpPost("accounts")]
        public async Task<object> CreateAccount(CreateAccountReq req)
        {
            //Logger.LogInformation("CreateAccount {@req}", req);

            var birthday  = DateTime.Parse(req.birthday);
            var accountId = Db.RegisterNewAccount(req.email, req.password, req.country, birthday, req.referral_key, req.email_optin);

            return new { };
        }
    }
}
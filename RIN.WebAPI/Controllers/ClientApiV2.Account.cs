﻿using System;
using System.Threading.Tasks;
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
        public async Task<LoginResp> Login()
        {
            var serverDefaults = Configuration.GetSection(ServerDefaultsSettings.NAME).Get<ServerDefaultsSettings>() ?? new ServerDefaultsSettings();
            
            Logger.LogInformation($"Headers: {GetHeadersDev()}");

            // Fill out the rest of the login data
            var loginData = new LoginResp
            {
                account_id = 1,
                can_login = true,
                is_dev = true,
                cais_status = new CaisStatus
                {
                    state      = "disabled",
                    duration   = 0,
                    expires_at = 0
                },
                
                steam_auth_prompt = false,
                skip_precursor    = false,
            };
            
            loginData.character_limit   = loginData.character_limit != -1 ? loginData.character_limit : serverDefaults.CharaterLimitPerAccount;

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
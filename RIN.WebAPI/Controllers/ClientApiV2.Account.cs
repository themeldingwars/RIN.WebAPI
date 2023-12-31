using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Web;
using FauFau.Net.Web;
using Microsoft.AspNetCore.Mvc;
using RIN.Core;
using RIN.WebAPI.Models.ClientApi;
using RIN.WebAPI.Utils;

namespace RIN.WebAPI.Controllers
{
    public partial class ClientApiV2
    {
        // Todo proper login
        [HttpPost("accounts/login")]
        public async Task<ActionResult<LoginResp>> Login()
        {
            ContentResult invalidLoginError = ReturnError(Error.Codes.ERR_INCORRECT_USERPASS, "Login failed, check your username and password");

            //Logger.LogInformation($"Headers: {GetHeadersDev()}");

            var uid = HttpUtility.UrlDecode(GetRed5Sig().UID.ToString());
            var loginResult = await Db.GetLoginData(uid);

            // Email not found
            if (loginResult == null) 
                return invalidLoginError;

            var authed = Auth.Verify(loginResult.secret, GetRed5Sig1Str().AsSpan());

            // Auth failed, password mismatch prob
            if (!authed)
                return invalidLoginError;

            LoginEvents loginEvents = GetLoginEvents();

            // Fill out the rest of the login data
            var loginData = new LoginResp
            {
                account_id        = loginResult.account_id,
                can_login         = true,
                events            = loginEvents,
                is_dev            = loginResult.is_dev,
                steam_auth_prompt = false,
                skip_precursor    = false,
                cais_status       = new CaisStatus
                {
                    state      = "disabled",
                    duration   = 0,
                    expires_at = 0
                },
                created_at      = new DateTimeOffset(loginResult.created_at).ToUnixTimeSeconds(),
                character_limit = loginResult.character_limit,
                is_vip          = false,
                vip_expiration  = -1,
            };

            loginData.character_limit = loginData.character_limit != -1 ? loginData.character_limit : ServerDefaults.CharaterLimitPerAccount;

            await Db.UpdateLastLoginTime(loginResult.account_id);

            return loginData;
        }

        private static LoginEvents GetLoginEvents()
        {
            // Generate fixed login events
            // TODO: Store events in database table webapi.LoginEvents and pull from there
            // When events should be deactivated they should have is_active set to false
            // Including them in this list even as active does not automatically result in them impacting the zones
            // Additional zone world handling controls their impact on the world and red5 always left them as "is_active=true" forever
            var loginEvents = new LoginEvents();
            var eventList = new List<LoginEvent>()
            {
                new LoginEvent(){ id = 821, name = "Valentine Day Event", description = "Valentine Day Event 2016", color = "#e21818", is_active = true, created_at = "2016-02-10T22:41:40+00:00", updated_at = "2016-02-19T22:23:23+00:00"},
                new LoginEvent(){ id = 721, name = "Lunar New Year", description = "Celebrating the Lunar New Year in New Eden!", color = "#e85314", is_active = true, created_at = "2016-02-05T00:40:23+00:00", updated_at = "2016-02-06T01:05:13+00:00"},
                new LoginEvent(){ id = 621, name = "Super Glider Challenge", description = "This is for all schedules glider challenge events", color = "#00dfff", is_active = true, created_at = "2015-01-12T22:15:19+00:00", updated_at = "2015-01-13T20:25:14+00:00"},
                new LoginEvent(){ id = 521, name = "Fireworks", description = "Schedule firework displays across the game. Note that some live_encounters may also include built in fireworks - check with your local live event designer to see if you need to explicitly schedule fireworks.", color = "#0ee031", is_active = true, created_at = "2014-12-23T22:27:16+00:00", updated_at = "2014-12-23T22:27:23+00:00"},
                new LoginEvent(){ id = 421, name = "Wintertide", description = "December holiday event", color = "#138c00", is_active = true, created_at = "2014-12-16T00:50:53+00:00", updated_at = "2014-12-16T00:51:00+00:00"},
                new LoginEvent(){ id = 321, name = "Night of the Melding", description = "Live event(s) for Halloween.", color = "#cc00ff", is_active = true, created_at = "2014-10-28T21:46:48+00:00", updated_at = "2014-10-28T21:46:54+00:00"},
                new LoginEvent(){ id = 221, name = "Asset Management", description = "Includes all banner rotations and other asset management tasks.", color = "#f9ff00", is_active = true, created_at = "2014-09-25T18:14:35+00:00", updated_at = "2014-09-25T18:14:41+00:00"},
                new LoginEvent(){ id = 121, name = "Crossfire", description = "Crossfire live event", color = "#ff8935", is_active = true, created_at = "2014-09-19T01:53:54+00:00", updated_at = "2014-09-19T01:59:45+00:00"},
                new LoginEvent(){ id = 021, name = "Chosen Offensive", description = "Devs as chosen bosses.", color = "#ff3030", is_active = true, created_at = "2014-09-19T01:47:35+00:00", updated_at = "2014-09-19T02:01:24+00:00"}
            };
            loginEvents.results.AddRange(eventList);
            return loginEvents;
        }

        [HttpPost("accounts")]
        public async Task<object> CreateAccount(CreateAccountReq req)
        {
            //Logger.LogInformation("CreateAccount {@req}", req);

            var birthday  = DateTime.Parse(req.birthday);
            var accountId = Db.RegisterNewAccount(req.email, req.password, req.country, birthday, req.referral_key, req.email_optin);

            return new { };
        }

        // Lists out each locked slot along with purchase cost that the account can unlock
        // TODO: Store these in the account database to pull from
        [HttpGet("accounts/character_slots")]
        [R5SigAuthRequired]
        public async Task<object> CharacterSlots()
        {
            var resp = new List<LockedSlots>();
            resp.Add(new LockedSlots() { rb_cost = 0 });
            resp.Add(new LockedSlots() { rb_cost = 0 });
            resp.Add(new LockedSlots() { rb_cost = 0 });
            return resp;
        }

        // Process the character slot unlocked purchase
        // If this does not return an error, the client will automatically add an additional character slot for the player
        // TODO: Handle incrementing the character_limit field in the database and subtract an entry in LockedSlots
        [HttpPost("accounts/character_slots")]
        [R5SigAuthRequired]
        public async Task<object> CharacterSlotsUnlock(LockedSlots req)
        {
            // TODO: Verify user can afford purchase and then remove red beans from account

            bool purchase_error = false;
            if (purchase_error)
                return ReturnError(Error.Codes.ERR_UNKNOWN, "Error completing purchase");

            return true;
        }
    }
}
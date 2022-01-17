using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RIN.WebAPI.Models;
using RIN.WebAPI.Models.ClientApi;
using RIN.WebAPI.Models.Config;

namespace RIN.WebAPI.Controllers
{
    [ApiController]
    [Route("Clientapi/api/v1")]
    public class ClientApiV1 : TmwController
    {
        private readonly IConfiguration              Configuration;
        private readonly ILogger<OperatorController> Logger;

        public ClientApiV1(IConfiguration configuration, ILogger<OperatorController> logger)
        {
            Configuration = configuration;
            Logger        = logger;
        }

        // Todo: log to db?
        [HttpPost("client_event")]
        public async Task<string> ClientEvent(ClientEvent evnt)
        {
            Logger.LogInformation("ClientEvent: {@evnt}", evnt);
            return "";
        }

        // Todo: read from DB
        [HttpGet("login_alerts")]
        public async Task<List<LoginAlert>> LoginAlerts()
        {
            var envStr = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var alerts = new List<LoginAlert>
            {
                new LoginAlert {message = $"RIN WebAPI {envStr}"},
                new LoginAlert {message = "Test login alert :D"}
            };

            return alerts;
        }

        [HttpPost("characters/validate_name")]
        public async Task<ValidateNameResp> ValidateName(ValidateNameReq nameData)
        {
            var serverDefaults = Configuration.GetSection(ServerDefaultsSettings.NAME).Get<ServerDefaultsSettings>() ?? new ServerDefaultsSettings();

            var data = new ValidateNameResp
            {
                name   = nameData.name,
                valid  = true,
                code   = "",
                reason = new List<string>()
            };

            if (nameData.name.Length > serverDefaults.CharaterNameMaxLength) data.reason.Add(Error.Codes.ERR_NAME_TOO_LONG);
            if (nameData.name.Length < serverDefaults.CharaterNameMinLength) data.reason.Add(Error.Codes.ERR_NAME_TOO_SHORT);

            if (data.reason.Count > 0) {
                data.valid = false;
                data.code  = Error.Codes.ERR_NAME_INVALID;
                return data;
            }

            // Todo: Check if name is in use or blocked

            return data;
        }
        
        // Todo
        [HttpPost("characters/{characterGuid}/delete")]
        public async Task<object> Delete(long characterGuid)
        {
            
            return new {valid = true};
        }

        // Todo
        [HttpPost("characters")]
        public async Task<object> Characters(CreateCharacterReq reqData)
        {

            return null;
        }

        [HttpPost("oracle/ticket")]
        public async Task<OracleTicket> OracleTicket(OracleTicketReq req)
        {
            var devServerSettings = Configuration.GetSection(DevServerSettings.NAME).Get<DevServerSettings>() ?? new DevServerSettings();

            if (devServerSettings.EnableLocalDev) {
                var ticket = new OracleTicket
                {
                    country           = "JP",
                    datacenter        = devServerSettings.DevGameServerURL.Split(":").Last(),
                    hostname          = devServerSettings.DevGameServerURL.Split(":").Last(),
                    matrix_url        = devServerSettings.DevGameServerURL,
                    session_id        = devServerSettings.DevSessionId,
                    ticket            = devServerSettings.DevTicket,
                    operator_override = null
                };

                return ticket;
            }

            // TODO: server lookup, instances, last zone and all that
            return null;
        }
    }
}
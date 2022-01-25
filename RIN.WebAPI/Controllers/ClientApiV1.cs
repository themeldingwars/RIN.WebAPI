using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RIN.WebAPI.DB.SDB;
using RIN.WebAPI.Models;
using RIN.WebAPI.Models.ClientApi;
using RIN.WebAPI.Models.Config;
using RIN.WebAPI.Utils;

namespace RIN.WebAPI.Controllers
{
    [ApiController]
    [Route("Clientapi/api/v1")]
    public class ClientApiV1 : TmwController
    {
        private readonly IConfiguration              Configuration;
        private readonly ILogger<OperatorController> Logger;
        private readonly DB.DB                       Db;
        private readonly SDB                         Sdb;

        public ClientApiV1(IConfiguration configuration, ILogger<OperatorController> logger, DB.DB db, SDB sdb)
        {
            Configuration = configuration;
            Logger        = logger;
            Db            = db;
            Sdb           = sdb;
        }

        // Todo: log to db?
        [HttpPost("client_event")]
        public async Task<string> ClientEvent(ClientEvent evnt)
        {
            Logger.LogInformation("ClientEvent: {@evnt}", evnt);
            await Db.LogClientEvent(evnt);
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
        
        [HttpPost("characters")]
        public async Task<object> Characters(CreateCharacterReq reqData)
        {
            // TODO: Validate item inputs like heads etc
            const byte DEFAULT_RACE = 0;

            using var tx          = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            var       loginResult = await Db.GetLoginData(GetUid()); // temp

            var colors     = await Sdb.GetNewCharactersColors(reqData.eye_color_id, reqData.skin_color_id, reqData.hair_color_id);
            var genderInt  = CharacterUtil.GenderStrToNum(reqData.gender);
            var visuals    = CharacterUtil.CreateVisualsObj(colors, DEFAULT_RACE, genderInt, reqData.eye_color_id, reqData.skin_color_id, reqData.hair_color_id, reqData.voice_set, reqData.head, reqData.head_accessory_a);
            var visualBlob = Utils.Utils.ToProtoBuffByteArray(visuals);

            var charId = await Db.CreateNewCharacter(loginResult.account_id, reqData.name, reqData.is_dev, reqData.voice_set, 0, visualBlob);

            tx.Complete();
            
            var createData = new CreateCharacterResp
            {
                created_at        = DateTime.Now,
                updated_at        = DateTime.Now,
                name              = reqData.name,
                head_accAId       = reqData.head_accessory_a,
                head_accBId       = 0,
                head_mainId       = reqData.head,
                is_active         = true,
                is_dev            = reqData.is_dev,
                max_frame_level   = 0,
                needs_name_change = false,
                pool_id           = 0,
                race              = 0, // always human here?
                time_played_secs  = 0,
                title_id          = 0,
                unique_name       = reqData.name.ToUpper(),
                voice_setId       = reqData.voice_set,
                gender            = reqData.gender
            };
            
            return createData;
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
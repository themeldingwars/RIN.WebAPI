﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly ServerDefaultsSettings         ServerDefaults;
        private readonly DevServerSettings              DevServerSettings;
        private readonly ILogger<OperatorController>    Logger;
        private readonly DB.DB                          Db;
        private readonly SDB                            Sdb;

        public ClientApiV1(
                IOptions<ServerDefaultsSettings> serverDefaults,
                IOptions<DevServerSettings> devServerSettings,
                ILogger<OperatorController> logger,
                DB.DB db,
                SDB sdb
            )
        {
            ServerDefaults    = serverDefaults.Value;
            DevServerSettings = devServerSettings.Value;
            Logger            = logger;
            Db                = db;
            Sdb               = sdb;
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
        [R5SigAuthRequired]
        public async Task<ValidateNameResp> ValidateName(ValidateNameReq nameData)
        {
            var data = new ValidateNameResp
            {
                name   = nameData.name,
                valid  = true,
                code   = "",
                reason = new List<string>()
            };

            if (nameData.name.Length > ServerDefaults.CharaterNameMaxLength) data.reason.Add(Error.Codes.ERR_NAME_TOO_LONG);
            if (nameData.name.Length < ServerDefaults.CharaterNameMinLength) data.reason.Add(Error.Codes.ERR_NAME_TOO_SHORT);

            if (data.reason.Count == 0 && Char.IsDigit(nameData.name[0]))
            {
                data.reason.Add(Error.Codes.ERR_NAME_STARTS_WITH_NUMBER);
            }

            if (data.reason.Count == 0 && CharacterUtil.IsInvalidCharactersInName(nameData.name))
            {
                data.reason.Add(Error.Codes.ERR_INVALID_CHARACTER);
            }

            // TODO: Check if name is blocked (reserved or contains profanity)
            // Both of these checks most likely should use new database tables that contains a list of blocked names
            var isfree_result = Db.CheckIfNameIsFree(nameData.name);

            if (isfree_result.Result == false)
            {
                data.reason.Add(Error.Codes.ERR_NAME_IN_USE);
            }

            if (data.reason.Count > 0)
            {
                data.valid = false;
                data.code  = Error.Codes.ERR_NAME_INVALID;
                return data;
            }

            return data;
        }

        // TODO: Setup a database script/system to automatically delete any characters with expire_in date reached
        [HttpPost("characters/{characterGuid}/delete")]
        [R5SigAuthRequired]
        public async Task<object> Delete(long characterGuid)
        {
            using var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            var loginResult = await Db.GetLoginData(GetUid());
            var delete_result = Db.SetPendingDeleteCharacterById(loginResult.account_id, characterGuid);

            tx.Complete();

            if (delete_result.Result.code == Error.Codes.SUCCESS)
            {
                return true;
            }
            else
            {
                return ReturnError(delete_result.Result, 404);
            }
        }

        // TODO: Game is pushing Key and Namespace through QueryStrings and then returning it, why?
        // TODO: What is Value and where does it come from?
        [HttpGet("characters/{characterGuid}/data")]
        [R5SigAuthRequired]
        public async Task<CharacterDataResp> Data(long characterGuid, [FromQuery] string key = "76336_0", [FromQuery] string @namespace = "bfAbiMap")
        {
            var characterData = new CharacterDataResp
            {
                Key = key,
                Namespace = @namespace,
                Value = "0,1,2,3"
            };

            return characterData;
        }

        [HttpPost("characters")]
        [R5SigAuthRequired]
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

            var charId = await Db.CreateNewCharacter(loginResult.account_id, reqData.name, reqData.is_dev, reqData.voice_set, genderInt, reqData.start_class_id, visualBlob);

            tx.Complete();
            
            var createData = new CreateCharacterResp
            {
                created_at        = DateTime.Now,
                updated_at        = DateTime.Now,
                name              = reqData.name,
                head_accAId       = reqData.head_accessory_a,
                head_accBId       = reqData.head_accessory_b,
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
        [R5SigAuthRequired]
        public async Task<OracleTicket> OracleTicket(OracleTicketReq req)
        {
            if (DevServerSettings.EnableLocalDev) {
                var ticket = new OracleTicket
                {
                    country           = "JP",
                    datacenter        = DevServerSettings.DevGameServerURL.Split(":").Last(),
                    hostname          = DevServerSettings.DevGameServerURL.Split(":").Last(),
                    matrix_url        = DevServerSettings.DevGameServerURL,
                    session_id        = DevServerSettings.DevSessionId,
                    ticket            = DevServerSettings.DevTicket,
                    operator_override = null
                };

                return ticket;
            }

            // TODO: server lookup, instances, last zone and all that
            return null;
        }

        // Zone List for devs
        [HttpPost("server/list")]
        [R5SigAuthRequired]
        public async Task<object> ServerList(ServerListReq req)
        {
            var zone_list = new ZoneList();
            var zone = new Zones()
            {
                match = "0",
                matrix_url = "https://localhost/test_matrix",
                zone_name = "test_zone",
                revision = "0",
                protocol_version = "0",
                owner = "root",
                players = 0
            };
            zone_list.zone_list.Add(zone);

            return zone_list;
        }
    }
}
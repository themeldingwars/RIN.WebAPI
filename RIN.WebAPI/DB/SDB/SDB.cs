﻿using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RIN.WebAPI.Models;
using RIN.WebAPI.Models.SDB;

namespace RIN.WebAPI.DB.SDB
{
    public class SDB : DbBase
    {
        public SDB(IConfiguration configuration, ILogger<SDB> logger) : base(configuration, logger)
        {
            var config = Configuration.GetSection(WebApiConfigSettings.NAME).Get<WebApiConfigSettings>();
            ConnStr    = config.SDBConnectionStr;
            LogDbTimes = config.LogDbCallTimes;
        }

        // For the passed color ids return the light and dark colors from the warpaints table
        public async Task<NewCharaterColors?> GetNewCharactersColors(int eyeColorId, int skinColorId, int hairColorId)
        {
            const string SELECT_SQL = @"SELECT * from 
                (SELECT color1_highlight as ""EyeColorLight"", color1_shadow as ""EyeColorDark""
                    FROM sdb.""dbvisualrecords::WarpaintPalette"" WHERE id = @eyeColorId) as eyeColor,
	    
                (SELECT color1_highlight as ""SkinColorLight"", color1_shadow as ""SkinColorDark""
                    FROM sdb.""dbvisualrecords::WarpaintPalette"" WHERE id = @skinColorId) as skinColor,
	    
                (SELECT color1_highlight as ""HairColorLight"", color1_shadow as ""HairColorDark""
                    FROM sdb.""dbvisualrecords::WarpaintPalette"" WHERE id = @hairColorId) as HairColor;";
            
            var result = await DBCall(conn => conn.QueryAsync<NewCharaterColors>(SELECT_SQL, new {eyeColorId, skinColorId, hairColorId}),
                exception => Logger.LogError($"Error getting colors for new character with ids (eye: {eyeColorId}, skin: {skinColorId}, hair: {hairColorId}) due to: {exception}"));

            return result.FirstOrDefault();
        }
    }
}
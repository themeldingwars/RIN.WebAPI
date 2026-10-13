using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RIN.Core;
using RIN.Core.Config;
using RIN.Core.SDB;

namespace RIN.Core.DB.SDB
{
    public class SDB : DbBase
    {
        public SDB(IOptions<DbConnectionSettings> config, ILogger<SDB> logger) : base(config, logger)
        {
            ConnStr    = config.Value.SDBConnStr;
            LogDbTimes = config.Value.LogDbCallTimes;
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

        // The client omits start_class_id when the player re-enters the creator from the customize screen.
        // Returns the requested frame when it is a starting loadout, otherwise the first starting loadout
        // (Accord Dreadnaught), so a character never ends up with battleframe_sdb_id 0, which breaks the
        // zone selection paperdoll ("Invalid item ID") and leaves "Enter World" dead.
        public async Task<int> GetStartingBattleframeId(int requestedFrameId)
        {
            const string SELECT_SQL = @"SELECT frame_id FROM sdb.""dbcharacter::CharCreateLoadout""
                WHERE is_starting_loadout = 1 AND is_dev = 0
                ORDER BY CASE WHEN frame_id = @requestedFrameId THEN 0 ELSE 1 END, id
                LIMIT 1;";

            var result = await DBCall(conn => conn.QueryAsync<int>(SELECT_SQL, new { requestedFrameId }),
                exception => Logger.LogError($"Error looking up starting battleframe for requested frame {requestedFrameId} due to: {exception}"));

            var frameId = result.FirstOrDefault();
            if (frameId != requestedFrameId)
                Logger.LogWarning("Requested start class {Requested} is not a starting loadout, using {Fallback}", requestedFrameId, frameId);

            return frameId;
        }

        public async Task<IEnumerable<CosmeticInfo>> GetOrnamentsInfoList()
        {
            const string SELECT_SQL = @"SELECT ornaments.id, lang.english AS lang_name, display_flags, ""Usage"" AS ""usage""
                FROM sdb.""dbvisualrecords::OrnamentsMapGroups"" AS ornaments
                LEFT JOIN sdb.""dblocalization::LocalizedText"" AS lang ON lang.id = ""localizedNameId""";

            var result = await DBCall(conn => conn.QueryAsync<CosmeticInfo>(SELECT_SQL, new { }));

            return result;
        }
    }
}
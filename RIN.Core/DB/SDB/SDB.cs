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

        public async Task<IEnumerable<CosmeticInfo>> GetOrnamentsInfoList()
        {
            const string SELECT_SQL = @"SELECT ornaments.id, lang.english AS lang_name, display_flags, ""Usage"" AS ""usage""
                FROM sdb.""dbvisualrecords::OrnamentsMapGroups"" AS ornaments
                LEFT JOIN sdb.""dblocalization::LocalizedText"" AS lang ON lang.id = ""localizedNameId""";

            var result = await DBCall(conn => conn.QueryAsync<CosmeticInfo>(SELECT_SQL, new { }));

            return result;
        }

        // The palettes the garage sells as chassis paint. type_flags 16 is "full body".
        public async Task<IEnumerable<CosmeticInfo>> GetWarpaintsInfoList()
        {
            const string SELECT_SQL = @"SELECT id
                FROM sdb.""dbvisualrecords::WarpaintPalette""
                WHERE type_flags = 16
                ORDER BY id";

            var result = await DBCall(conn => conn.QueryAsync<CosmeticInfo>(SELECT_SQL, new { }));

            return result;
        }

        // Warpaint patterns (camo, stripes, gradients) the garage sells.
        public async Task<IEnumerable<CosmeticInfo>> GetCziPatternsInfoList()
        {
            const string SELECT_SQL = @"SELECT patterns.id, lang.english AS lang_name
                FROM sdb.""dbvisualrecords::CziPattern"" AS patterns
                LEFT JOIN sdb.""dblocalization::LocalizedText"" AS lang ON lang.id = patterns.name_id
                ORDER BY patterns.id";

            var result = await DBCall(conn => conn.QueryAsync<CosmeticInfo>(SELECT_SQL, new { }));

            return result;
        }

        // Decals / tattoos the garage sells.
        public async Task<IEnumerable<CosmeticInfo>> GetTattooDecalsInfoList()
        {
            const string SELECT_SQL = @"SELECT decals.id, lang.english AS lang_name
                FROM sdb.""dbvisualrecords::TattooDecal"" AS decals
                LEFT JOIN sdb.""dblocalization::LocalizedText"" AS lang ON lang.id = decals.localized_name_id
                ORDER BY decals.id";

            var result = await DBCall(conn => conn.QueryAsync<CosmeticInfo>(SELECT_SQL, new { }));

            return result;
        }

        /// <summary>
        /// Turn a stored visual set into the web API's warpaint colours.
        /// </summary>
        public async Task<IEnumerable<WarpaintPaletteColors>> GetWarpaintPalettesInfo(int[] paletteIds)
        {
            if (paletteIds == null || paletteIds.Length == 0)
                return new List<WarpaintPaletteColors>();

            const string SELECT_SQL = @"SELECT id, type_flags,
                                                color1_highlight, color1_shadow,
                                                color2_highlight, color2_shadow,
                                                color3_highlight, color3_shadow,
                                                color4_highlight, color4_shadow,
                                                color5_highlight, color5_shadow,
                                                color6_highlight, color6_shadow,
                                                color7_highlight, color7_shadow
                FROM sdb.""dbvisualrecords::WarpaintPalette""
                WHERE id = ANY(@paletteIds)";

            var result = await DBCall(conn => conn.QueryAsync<WarpaintPaletteColors>(SELECT_SQL, new { paletteIds }));

            return result;
        }

        /// <summary>
        /// The palettes these chassis start with.
        /// </summary>
        public async Task<IEnumerable<BattleframePaletteDefaults>> GetBattleframeDefaultPalettes(int[] chassisSdbIds)
        {
            if (chassisSdbIds == null || chassisSdbIds.Length == 0)
                return new List<BattleframePaletteDefaults>();

            const string SELECT_SQL = @"SELECT id AS battleframe_sdb_id,
                                                default_fullbody_palette_id,
                                                default_armor_palette_id,
                                                default_bodysuit_palette_id,
                                                default_glow_palette_id
                FROM sdb.""dbitems::Battleframe""
                WHERE id = ANY(@chassisSdbIds)";

            var result = await DBCall(conn => conn.QueryAsync<BattleframePaletteDefaults>(SELECT_SQL, new { chassisSdbIds }));

            return result;
        }
    }
}
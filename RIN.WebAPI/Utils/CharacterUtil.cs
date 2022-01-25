using System;
using System.Collections.Generic;
using RIN.WebAPI.Models.ClientApi;
using RIN.WebAPI.Models.Common;
using RIN.WebAPI.Models.DB;
using RIN.WebAPI.Models.SDB;

namespace RIN.WebAPI.Utils
{
    public class CharacterUtil
    {
        public static CharacterVisuals CreateVisualsObj(NewCharaterColors colors, byte race, byte gender, int eyeColorId, int skinColorId, int hairColorId, int voiceSet, int headId, int hairId, int facialHairId = 0, int eyesId = 10001)
        {
            var visuals = new CharacterVisuals
            {
                race              = race,
                gender            = gender,
                skin_color        = new WebIdValueColor(skinColorId, colors.SkinColor),
                voice_set         = new WebId(voiceSet),
                head              = new WebId(headId),
                eye_color         = new WebIdValueColor(eyeColorId, colors.EyeColor),
                lip_color         = new WebIdValueColor(0, 0),
                hair_color        = new WebIdValueColor(hairColorId, colors.HairColor),
                facial_hair_color = new WebIdValueColor(hairColorId, colors.HairColor),
                head_accessories  = new List<WebIdValueColor> {new(hairId, colors.HairColor)},
                ornaments         = new List<WebId>(),
                eyes              = new WebId(eyesId),
                hair              = new WebIdValueColorId(hairId, hairColorId, colors.HairColor),
                facial_hair       = new WebIdValueColorId(facialHairId, hairColorId, colors.HairColor),
                glider            = new WebId(0),
                vehicle           = new WebId(0)
            };

            return visuals;
        }
        
        public static byte GenderStrToNum(string gender) => string.Equals(gender, "female", StringComparison.InvariantCultureIgnoreCase) ? (byte)1 : (byte)0;

        public static string GenderNumToString(int gender)
        {
            var genderStr = gender switch
            {
                0 => "female",
                1 => "male"
            };

            return genderStr;
        }

        public static string RaceIdToString(int id)
        {
            var race = id switch
            {
                0  => "human",
                2  => "dark_one",
                6  => "monster",
                7  => "friendly",
                8  => "melding",
                9  => "gaea",
                10 => "bandit",
                _  => "human"
            };

            return race;
        }
    }
}
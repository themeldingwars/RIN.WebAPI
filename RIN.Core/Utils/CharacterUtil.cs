using System.Text.RegularExpressions;
using RIN.Core.Common;
using RIN.Core.DB;
using RIN.Core.Models;
using RIN.Core.SDB;

namespace RIN.Core.Utils
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
                0 => "male",
                1 => "female"
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

        // Game client does not allow for names to contain Control or Punctuation characters
        // Only allows a name to contain the following characters a-z, A-Z, 0-9, and ' '
        private static readonly Regex ValidChars = new Regex(@"^[a-zA-Z0-9 ]*$", RegexOptions.Compiled | RegexOptions.Singleline);

        public static bool IsInvalidCharactersInName(string name)
        {
            return !ValidChars.IsMatch(name);
        }

        public static CharacterVisuals UpdateCharacterVisualsFromGarage(CharacterVisuals cv, PlayerVisualLoadout updates, NewCharaterColors colors)
        {
            cv.race             = updates.race;
            cv.gender           = updates.gender;
            cv.voice_set.id     = updates.voice_set_id;
            cv.head.id          = updates.head_id;
            cv.lip_color.id     = updates.lip_color_id;
            cv.facial_hair.id   = updates.facial_hair_color_id;
            cv.eyes.id          = updates.eye_id;

            if (cv.skin_color.id != updates.skin_color_id)
            {
                cv.skin_color.id = updates.skin_color_id;
                cv.skin_color.value.color = colors.SkinColor;
            }

            if (cv.eye_color.id != updates.eye_color_id)
            {
                cv.eye_color.id = updates.eye_color_id;
                cv.eye_color.value.color = colors.SkinColor;
            }

            if (cv.hair.id != updates.hair_id)
            {
                cv.hair.id = updates.hair_id;
            }

            if (cv.facial_hair.id != updates.facial_hair_id)
            {
                cv.facial_hair.id = updates.facial_hair_id;
            }

            if (cv.hair_color.id != updates.hair_color_id)
            {
                cv.hair_color.id                  = updates.hair_color_id;
                cv.hair_color.value.color         = colors.HairColor;
                cv.hair.color.value               = colors.HairColor;

                cv.facial_hair.id                 = updates.hair_color_id;
                cv.facial_hair.color.value        = colors.HairColor;
                cv.facial_hair_color.value.color  = colors.HairColor;
            }

            cv.ornaments.Clear();
            foreach (var ornament in updates.ornaments)
            {
                cv.ornaments.Add(new WebId(ornament.remote_id));
            }

            return cv;
        }
    }
}
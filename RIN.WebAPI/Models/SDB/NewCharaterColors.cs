using RIN.WebAPI.Utils;

namespace RIN.WebAPI.Models.SDB
{
    public class NewCharaterColors
    {
        public long EyeColorLight  { get; set; }
        public long EyeColorDark   { get; set; }
        public long SkinColorLight { get; set; }
        public long SkinColorDark  { get; set; }
        public long HairColorLight { get; set; }
        public long HairColorDark  { get; set; }

        public uint EyeColor
        {
            get => FColor.CombineLightDark((uint)EyeColorLight, (uint)EyeColorDark);
        }

        public uint SkinColor
        {
            get => FColor.CombineLightDark((uint)SkinColorLight, (uint)SkinColorDark);
        }

        public uint HairColor
        {
            get => FColor.CombineLightDark((uint)HairColorLight, (uint)HairColorDark);
        }
    }
}
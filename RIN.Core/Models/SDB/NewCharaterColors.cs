using FauFau.Util;

namespace RIN.Core.SDB
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
            get => WarpaintColor.Pack((uint)EyeColorLight, (uint)EyeColorDark);
        }

        public uint SkinColor
        {
            get => WarpaintColor.Pack((uint)SkinColorLight, (uint)SkinColorDark);
        }

        public uint HairColor
        {
            get => WarpaintColor.Pack((uint)HairColorLight, (uint)HairColorDark);
        }
    }
}
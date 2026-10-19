using FauFau.Util;

namespace RIN.Core.SDB
{
    public class WarpaintPaletteColors
    {
        // dbvisualrecords::WarpaintPalette.type_flags bits naming the chassis region a palette paints.
        public const uint ArmorRegion    = 1 << 0;
        public const uint BodysuitRegion = 1 << 1;
        public const uint GlowRegion     = 1 << 3;
        public const uint FullbodyRegion = 1 << 4;

        public long id         { get; set; }
        public long type_flags { get; set; }

        public long color1_highlight { get; set; }
        public long color1_shadow    { get; set; }
        public long color2_highlight { get; set; }
        public long color2_shadow    { get; set; }
        public long color3_highlight { get; set; }
        public long color3_shadow    { get; set; }
        public long color4_highlight { get; set; }
        public long color4_shadow    { get; set; }
        public long color5_highlight { get; set; }
        public long color5_shadow    { get; set; }
        public long color6_highlight { get; set; }
        public long color6_shadow    { get; set; }
        public long color7_highlight { get; set; }
        public long color7_shadow    { get; set; }

        /// <summary>
        /// The palette's seven colours in the order the web API's warpaint array and the game wire use:
        /// armor 1-3, bodysuit 1-2, glow 1-2.
        /// </summary>
        public uint[] PackColors()
        {
            return new[]
            {
                WarpaintColor.Pack((uint) color1_highlight, (uint) color1_shadow),
                WarpaintColor.Pack((uint) color2_highlight, (uint) color2_shadow),
                WarpaintColor.Pack((uint) color3_highlight, (uint) color3_shadow),
                WarpaintColor.Pack((uint) color4_highlight, (uint) color4_shadow),
                WarpaintColor.Pack((uint) color5_highlight, (uint) color5_shadow),
                WarpaintColor.Pack((uint) color6_highlight, (uint) color6_shadow),
                WarpaintColor.Pack((uint) color7_highlight, (uint) color7_shadow)
            };
        }
    }
}

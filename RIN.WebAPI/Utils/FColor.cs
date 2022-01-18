namespace RIN.WebAPI.Utils
{
    // Firefall warpaint color fun
    public class FColor
    {
        public static uint   CombineLightDark(uint light, uint dark) => ARGB8888ToRGB565(dark) | (uint) (ARGB8888ToRGB565(light) << 16);
        public static uint   ExtractLight(uint     combined) => RGB565ToARGB8888((ushort) (combined >> 16));
        public static uint   ExtractDark(uint      combined) => RGB565ToARGB8888((ushort) combined);
        public static ushort ARGB8888ToRGB565(uint argb)     => (ushort) (((((byte) (argb >> 16) >> 3) & 0x1f) << 11) | ((((byte) (argb >> 8) >> 2) & 0x3f) << 5) | (((byte) argb >> 3) & 0x1f));

        public static uint RGB565ToARGB8888(ushort rgb)
        {
            int r = (rgb            >> 11) * 255 + 16;
            int g = ((rgb & 0x07E0) >> 5)  * 255 + 32;
            int b = (rgb & 0x001F)         * 255 + 16;
            return (uint) (0xFF000000 | (byte) ((r / 32 + r) / 32) << 16 | (byte) ((g / 64 + g) / 64) << 8 | (byte) ((b / 32 + b) / 32));
        }
    }
}
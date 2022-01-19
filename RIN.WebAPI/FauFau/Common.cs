using System;

namespace FauFau.Util
{
    public static class Common
    {
        public static ReadOnlySpan<char> BytesToHexString(ReadOnlySpan<byte> hexBytes, bool upperCase = true)
        {
            int length = hexBytes.Length * 2;

            var str = string.Create(length, (length, hexBytes.ToArray(), upperCase), (chars, state) =>
            {
                const string HEX_VALUES       = "0123456789ABCDEF";
                const string HEX_VALUES_LOWER = "0123456789abcdef";
                var          values           = state.upperCase ? HEX_VALUES.AsSpan() : HEX_VALUES_LOWER.AsSpan();

                int srcIdx = 0;
                for (int i = 0; i < state.length; i += 2) {
                    chars[i]     = values[state.Item2[srcIdx] >> 4];
                    chars[i + 1] = values[state.Item2[srcIdx] & 0xF];
                    srcIdx++;
                }
            });
            
            return str.AsSpan();
        }
        
        public static char[] BytesToHexChars(ReadOnlySpan<byte> hexBytes, bool upperCase = true)
        {
            ReadOnlySpan<char> values = upperCase ? "0123456789ABCDEF" : "0123456789abcdef";
            char[] ret = new char[hexBytes.Length * 2];
            
            for (int i = 0; i < hexBytes.Length; i++)
            {
                ret[i*2]     = values[hexBytes[i] >> 4];
                ret[(i*2)+1] = values[hexBytes[i] & 0xF];
            }
            return ret;
        }
        
        public static bool TryWriteBytesAsHex(ReadOnlySpan<byte> input, Span<char> output, bool upperCase = true)
        {
            if (output.Length < input.Length * 2)
                return false;
            
            ReadOnlySpan<char> values = upperCase ? "0123456789ABCDEF" : "0123456789abcdef";

            for (int i = 0; i < input.Length; i++)
            {
                output[i*2]     = values[input[i] >> 4];
                output[(i*2)+1] = values[input[i] & 0xF];
            }

            return true;
        }
        
    }
}

using System.IO;
using Microsoft.Extensions.Options;
using ProtoBuf;

namespace RIN.Core.Utils
{
    public class MiscUtils
    {

        public static byte[] ToProtoBuffByteArray(object obj)
        {
            using var stream = new MemoryStream();
            Serializer.Serialize(stream, obj);
            stream.Flush();

            return stream.ToArray();
        }

        public static T FromProtoBuffByteArray<T>(ReadOnlySpan<byte> data)
        {
            var obj = Serializer.Deserialize<T>(data);

            return obj;
        }
    }
}
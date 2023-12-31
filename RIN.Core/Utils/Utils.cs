using System.IO;
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
    }
}
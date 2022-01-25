using System.IO;
using ProtoBuf;

namespace RIN.WebAPI.Utils
{
    public class Utils
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
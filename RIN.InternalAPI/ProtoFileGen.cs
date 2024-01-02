using ProtoBuf.Grpc.Reflection;
using ProtoBuf.Meta;
using RIN.InternalAPI;

namespace RIN.InternalAPI
{
    public class ProtoFileGen
    {
        public static void CreateProtoFiles()
        {
            GenServiceSchema<IGameServerAPI>();
        }

        public static void GenServiceSchema<T>()
        {
            var generator = new SchemaGenerator
            {
                ProtoSyntax = ProtoSyntax.Proto3
            };

            var schema   = generator.GetSchema<T>();
            var fileName = $"{typeof(T).Name}";
            if (fileName.StartsWith('I'))
                fileName = fileName.Substring(1);

            using (var writer = new System.IO.StreamWriter($"protos/{fileName}.proto"))
            {
                writer.Write(schema);
            }
        }
    }
}

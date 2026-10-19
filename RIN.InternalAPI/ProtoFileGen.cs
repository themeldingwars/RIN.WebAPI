using ProtoBuf.Grpc.Reflection;
using ProtoBuf.Meta;

namespace RIN.InternalAPI
{
    public class ProtoFileGen
    {
        private const string ProtoOutputDir = "generated_protos";

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

            System.IO.Directory.CreateDirectory(ProtoOutputDir);

            using (var writer = new System.IO.StreamWriter($"{ProtoOutputDir}/{fileName}.proto"))
            {
                writer.Write(schema);
            }
        }
    }
}

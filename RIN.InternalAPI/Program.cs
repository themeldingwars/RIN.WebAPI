using ProtoBuf.Grpc.Server;
using RIN.InternalAPI.Services;
using Serilog;

namespace RIN.InternalAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
#if DEBUG
            ProtoFileGen.CreateProtoFiles();
#endif

            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

            // Add services to the container.
            builder.Services.AddGrpc();
            builder.Services.AddCodeFirstGrpc();

            var app = builder.Build();
            app.UseSerilogRequestLogging();

            // Configure the HTTP request pipeline.
            app.MapGrpcService<GameServerAPI>();
            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

            app.Run();
        }
    }
}
using Grpc.Core;
using ProtoBuf.Grpc.Configuration;
using RIN.InternalAPI.Models;

namespace RIN.InternalAPI
{
    [Service("RIN.GameServerAPI")]
    public interface IGameServerAPI
    {
        public ValueTask<PingResp> Ping(PingReq req);
        public ValueTask<CharacterAndBattleframeVisuals> GetCharacterAndBattleframeVisuals(CharacterID req);

        // protobuf-net.BuildTools only knows the code first signatures, the raw Grpc.Core duplex shape is still supported at runtime
#pragma warning disable PBN2007
        public Task Stream(IAsyncStreamReader<Command> commands, IServerStreamWriter<Event> events, ServerCallContext context);
#pragma warning restore PBN2007
    }
}

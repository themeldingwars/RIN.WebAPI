using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;
using RIN.InternalAPI.Models;

namespace RIN.InternalAPI
{
    [Service("RIN.GameServerAPI")]
    public interface IGameServerAPI
    {
        public ValueTask<PingResp> Ping(PingReq req);
        public ValueTask<CharacterAndBattleframeVisuals> GetCharacterAndBattleframeVisuals(CharacterID req);
        public ValueTask<CharacterUnlocksResponse> GetCharacterUnlocks(CharacterID req);
        public ValueTask<TransferCharacterResp> TransferCharacter(TransferCharacterReq req);
        public ValueTask<CharacterInventoryResponse> GetCharacterInventory(CharacterID req);
        public ValueTask<BattleframeProgressionResponse> GetBattleframeProgression(CharacterID req);
        public IAsyncEnumerable<Event> Stream(IAsyncEnumerable<Command> commands, CallContext context = default);
    }
}

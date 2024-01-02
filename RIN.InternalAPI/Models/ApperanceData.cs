using ProtoBuf;
using RIN.Core.DB;
using RIN.Core.Models;

namespace RIN.InternalAPI.Models
{
    [ProtoContract]
    public class CharacterID
    {
        [ProtoMember(1)]
        public long ID { get; set; }
    }

    [ProtoContract]
    public class CharacterAndBattleframeVisuals
    {
        [ProtoMember(1)] public BasicCharacterInfo CharacterInfo { get; set; }
        [ProtoMember(2)] public CharacterVisuals CharacterVisuals { get; set; }
        [ProtoMember(3)] public PlayerBattleframeVisuals BattleframeVisuals { get; set; }
    }
}

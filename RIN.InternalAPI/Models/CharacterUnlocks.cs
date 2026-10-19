using ProtoBuf;
using RIN.Core.Models.DB;

namespace RIN.InternalAPI.Models
{
    [ProtoContract]
    public class CharacterUnlocksResponse
    {
        [ProtoMember(1)] public List<CharacterUnlockEntry> Unlocks { get; set; } = new();
    }
}

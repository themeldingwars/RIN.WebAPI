using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RIN.Core.Models
{
    [ProtoContract]
    public class BasicCharacterInfo
    {
        [ProtoMember(1)] public string Name { get; set; }
        [ProtoMember(2)] public byte Race { get; set; }
        [ProtoMember(3)] public byte Gender { get; set; }
        [ProtoMember(4)] public int TitleId { get; set; }
        [ProtoMember(5)] public long CurrentBattleframeId { get; set; }
    }
}

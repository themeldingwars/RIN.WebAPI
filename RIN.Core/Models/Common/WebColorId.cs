using ProtoBuf;

namespace RIN.Core.Common
{
    [ProtoContract]
    public class WebColorId
    {
        [ProtoMember(1)] public int  id    { get; set; }
        [ProtoMember(2)] public uint value { get; set; }

        public WebColorId()
        {
        }

        public WebColorId(int id, uint color)
        {
            this.id    = id;
            this.value = color;
        }
    }
}
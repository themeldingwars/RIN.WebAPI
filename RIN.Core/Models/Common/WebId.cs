using ProtoBuf;

namespace RIN.Core.Common
{
    [ProtoContract]
    public class WebId
    {
        [ProtoMember(1)] public int id { get; set; }

        public WebId()
        {
        }

        public WebId(int id)
        {
            this.id = id;
        }
    }
}
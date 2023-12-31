using ProtoBuf;

namespace RIN.Core.Common
{
    [ProtoContract]
    public class WebIdValueColorId
    {
        [ProtoMember(1)] public int        id    { get; set; }
        [ProtoMember(2)] public WebColorId color { get; set; }

        public WebIdValueColorId()
        {
        }

        public WebIdValueColorId(int id, int colorId, uint color)
        {
            this.id    = id;
            this.color = new WebColorId(colorId, color);
        }
    }
}
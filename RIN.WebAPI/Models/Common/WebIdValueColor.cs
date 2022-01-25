using ProtoBuf;

namespace RIN.WebAPI.Models.Common
{
    [ProtoContract]
    public class WebIdValueColor
    {
        [ProtoMember(1)] public int      id    { get; set; }
        [ProtoMember(2)] public WebColor value { get; set; }

        public WebIdValueColor()
        {
        }

        public WebIdValueColor(int id, uint color)
        {
            this.id = id;
            value   = new WebColor(color);
        }
    }
}
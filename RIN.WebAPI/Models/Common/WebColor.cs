using ProtoBuf;

namespace RIN.WebAPI.Models.Common
{
    [ProtoContract]
    public class WebColor
    {
        [ProtoMember(1)] public uint color { get; set; }

        public WebColor()
        {
        }

        public WebColor(uint color)
        {
            this.color = color;
        }
    }
}
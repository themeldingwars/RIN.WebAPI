using ProtoBuf;

namespace RIN.InternalAPI.Models
{
    [ProtoContract]
    public class PingReq
    {
        [ProtoMember(1, DataFormat = DataFormat.WellKnown)]
        public DateTime SentTime { get; set; }
    }

    [ProtoContract]
    public class PingResp
    {
        [ProtoMember(1, DataFormat = DataFormat.WellKnown)]
        public DateTime ClientSentTime { get; set; }

        [ProtoMember(2, DataFormat = DataFormat.WellKnown)]
        public DateTime ServerReciveTime { get; set; }
    }
}

using ProtoBuf;

namespace RIN.WebAPI.Models.User
{
    // Store soem data for a user ascross requests
    [ProtoContract]
    public class UserSessionData
    {
        [ProtoMember(1)] public long AccountId { get; set; }
        [ProtoMember(2)] public bool IsDev { get; set; }              = false;
        [ProtoMember(3)] public string Secert { get; set; }           = string.Empty;
        [ProtoMember(4)] public DateTime LastActiveTime { get; set; } = DateTime.Now;
    }
}

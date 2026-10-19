using System;
using ProtoBuf;

namespace RIN.InternalAPI.Models
{
    [ProtoContract]
    public class LoginAuthorizationRequest
    {
        [ProtoMember(1)] public byte[] Ticket { get; set; } = Array.Empty<byte>();
        [ProtoMember(2)] public ulong CharacterGuid { get; set; }
    }

    [ProtoContract]
    public class LoginAuthorizationResponse
    {
        [ProtoMember(1)] public bool Authorized { get; set; }
        [ProtoMember(2)] public string Reason { get; set; } = string.Empty;
        [ProtoMember(3)] public long AccountId { get; set; }
        [ProtoMember(4)] public bool IsDev { get; set; }
        [ProtoMember(5)] public int CharacterLimit { get; set; }
        [ProtoMember(6)] public ulong CharacterGuid { get; set; }
        [ProtoMember(7)] public string CharacterName { get; set; } = string.Empty;
        [ProtoMember(8)] public bool CharacterIsDev { get; set; }
    }

    public static class LoginAuthorizationReasons
    {
        public const string OK                       = "OK";
        public const string TICKET_MALFORMED         = "TICKET_MALFORMED";          // not base64, no header, or wrong length
        public const string TICKET_UNKNOWN           = "TICKET_UNKNOWN";            // well formed, but no account holds it
        public const string CHARACTER_NOT_FOUND      = "CHARACTER_NOT_FOUND";
        public const string CHARACTER_NOT_ON_ACCOUNT = "CHARACTER_NOT_ON_ACCOUNT";
        public const string CHARACTER_DELETED        = "CHARACTER_DELETED";
        public const string DB_ERROR                 = "DB_ERROR";                  // could not reach the database
    }
}

using ProtoBuf;

namespace RIN.InternalAPI.Models
{
    [ProtoContract]
    public class CharacterFriendEntry
    {
        [ProtoMember(1)] public ulong  CharacterGuid { get; set; }
        [ProtoMember(2)] public string Name          { get; set; } = string.Empty;
        [ProtoMember(3)] public string Note          { get; set; } = string.Empty;
        [ProtoMember(4)] public long   LastSeenAt    { get; set; }  // unix seconds

        // The game client's friend status: 1 friends, 2 a request this character sent, 3 a request it received
        [ProtoMember(5)] public uint   Status        { get; set; }

        // Zoned in on any game server that reported it
        [ProtoMember(6)] public bool   IsOnline      { get; set; }
    }

    [ProtoContract]
    public class CharacterFriendsResponse
    {
        [ProtoMember(1)] public List<CharacterFriendEntry> Friends { get; set; } = new();
    }

    [ProtoContract]
    public class UpdateCharacterFriendReq
    {
        [ProtoMember(1)] public ulong  CharacterGuid { get; set; }
        [ProtoMember(2)] public string FriendName    { get; set; } = string.Empty;
        [ProtoMember(3)] public int    Action        { get; set; }  // see UpdateCharacterFriendActions

        // Without a note an add keeps the note the friend already has
        [ProtoMember(4)] public bool   HasNote       { get; set; }
        [ProtoMember(5)] public string Note          { get; set; } = string.Empty;
    }

    [ProtoContract]
    public class UpdateCharacterFriendResp
    {
        [ProtoMember(1)] public string                Reason         { get; set; } = string.Empty;
        [ProtoMember(2)] public CharacterFriendEntry? Friend         { get; set; }  // the other character, set unless the name matched nobody
        [ProtoMember(3)] public FriendPresence?       FriendPresence { get; set; }  // the other character's presence while it is online
    }

    public static class UpdateCharacterFriendActions
    {
        public const int ADD      = 1;   // sends a request, or accepts the friend's request
        public const int SET_NOTE = 0;   // only replaces the note, the friendship stays as it is
        public const int REMOVE   = -1;  // ends a friendship, takes back a request or declines one
    }

    public static class UpdateCharacterFriendReasons
    {
        public const string REQUESTED = "REQUESTED";  // a friend request was sent
        public const string ACCEPTED  = "ACCEPTED";   // the add answered the friend's own request, both are friends now
        public const string UNCHANGED = "UNCHANGED";  // a note was set, or the add found a request or friendship already there
        public const string REMOVED   = "REMOVED";    // a friendship ended
        public const string CANCELLED = "CANCELLED";  // a request the character sent was taken back
        public const string DECLINED  = "DECLINED";   // a request the character received was turned down
        public const string NOT_FOUND = "NOT_FOUND";  // no character goes by that name, or the name isn't on the list for a removal or a note
        public const string SELF      = "SELF";
        public const string INVALID_ACTION = "INVALID_ACTION";
        public const string DB_ERROR  = "DB_ERROR";
    }

    /// <summary>
    /// What the friends of an online character see of it, as the game server hosting it reported it. RIN only keeps
    /// it while the character is online and passes it on.
    /// </summary>
    [ProtoContract]
    public class FriendPresence
    {
        [ProtoMember(1)] public uint  Gender         { get; set; }
        [ProtoMember(2)] public uint  ChassisId      { get; set; }
        [ProtoMember(3)] public uint  Level          { get; set; }
        [ProtoMember(4)] public uint  EffectiveLevel { get; set; }
        [ProtoMember(5)] public uint  EliteRank      { get; set; }
        [ProtoMember(6)] public ulong InstanceId     { get; set; }
        [ProtoMember(7)] public uint  ZoneId         { get; set; }
        [ProtoMember(8)] public uint  MaxHealth      { get; set; }
    }

    /// <summary>
    /// A game server reporting that a character it hosts came online, moved, or went offline. Sent over the stream,
    /// so a game server that goes away takes the presence of its characters with it.
    /// </summary>
    [ProtoContract]
    public class SetFriendPresence : Command
    {
        [ProtoMember(1)] public ulong           CharacterGuid { get; set; }
        [ProtoMember(2)] public string          Name          { get; set; } = string.Empty;
        [ProtoMember(3)] public FriendPresence? Presence      { get; set; }  // null when the character went offline

        // Also send the character the presence of its online friends. Only once its client has the friends list: the
        // UI makes an entry from presence that arrives before the list, and then keeps that one, without the note.
        [ProtoMember(4)] public bool            SendFriends   { get; set; }

        // A game server reporting its players again for a new stream: none of them just logged in
        [ProtoMember(5)] public bool            Resync        { get; set; }

        // Going offline in a way the character may come straight back from, a zone transfer, a timeout or a crash,
        // so it isn't announced right away
        [ProtoMember(6)] public bool            MayReturn     { get; set; }
    }

    /// <summary>
    /// Something a character's friends lists or presence did to other characters. Every game server gets it and the
    /// ones hosting a target tell that player.
    /// </summary>
    [ProtoContract]
    public class FriendNotification : Event
    {
        [ProtoMember(1)] public List<ulong>     TargetGuids  { get; set; } = new();
        [ProtoMember(2)] public uint            Kind         { get; set; }  // see FriendNotificationKinds
        [ProtoMember(3)] public ulong           FromGuid     { get; set; }
        [ProtoMember(4)] public string          FromName     { get; set; } = string.Empty;
        [ProtoMember(5)] public FriendPresence? FromPresence { get; set; }  // with ONLINE, and with ACCEPTED while the sender is online
    }

    // What happened, from the targets' side
    public static class FriendNotificationKinds
    {
        public const uint REQUEST  = 1;  // the sender asked the target to be friends
        public const uint DECLINED = 2;  // the sender turned down the target's request
        public const uint REMOVED  = 3;  // the sender ended the friendship or took back its request
        public const uint ACCEPTED = 4;  // the sender accepted the target's request
        public const uint ONLINE   = 5;  // the sender is online, or moved, see FromPresence
        public const uint OFFLINE  = 6;  // the sender went offline

        // A line in the targets' chat, only when the sender logs in or out, not when its presence just changes
        public const uint FRIEND_LOGIN  = 7;  // a friend logged in
        public const uint FRIEND_LOGOUT = 8;  // a friend logged out
        public const uint ARMY_LOGIN    = 9;  // an army mate that isn't a friend logged in
        public const uint ARMY_LOGOUT   = 10; // an army mate that isn't a friend logged out
    }
}

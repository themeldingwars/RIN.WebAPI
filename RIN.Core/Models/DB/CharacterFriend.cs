namespace RIN.Core.DB
{
    /// <summary>
    /// Outcome of adding or removing a friend. Error means the database could not be reached.
    /// </summary>
    public enum FriendUpdateStatus
    {
        Error     = 0,
        NotFound  = 1,  // no character goes by that name, or a removal of someone who is not on the list
        Self      = 2,  // a character can't be its own friend
        Requested = 3,  // the add sent a friend request
        Accepted  = 4,  // the add answered the friend's own request, both are friends now
        Unchanged = 5,  // the add found a request or friendship already there, only the note can have changed
        Removed   = 6,  // a friendship ended, on both lists
        Cancelled = 7,  // the owner took back a request they had sent
        Declined  = 8   // the owner turned down a request they had received
    }

    /// <summary>
    /// The game client's friend status for an entry on a character's list, as CharacterFriends stores it. A pending
    /// request is RequestSent on the sender's row and RequestReceived on the other.
    /// </summary>
    public static class CharacterFriendStatus
    {
        public const short Friend          = 1;
        public const short RequestSent     = 2;  // the client calls it PENDING_IN
        public const short RequestReceived = 3;  // the client calls it PENDING_OUT and shows the invite for it
    }

    // One entry on a character's friends list
    public class CharacterFriendRow
    {
        public long   friend_guid  { get; set; }
        public string name         { get; set; } = string.Empty;
        public short  status       { get; set; }
        public string note         { get; set; } = string.Empty;
        public long   last_seen_at { get; set; }  // unix seconds
    }
}

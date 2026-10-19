namespace RIN.Core.DB
{
    /// <summary>
    /// Outcome of checking a presented login ticket. Error means the database could not be reached, which is not the same as the ticket being wrong.
    /// </summary>
    public enum LoginTicketStatus
    {
        Error     = 0,
        Valid     = 1,
        Malformed = 2,
        Unknown   = 3  // well formed, but no account holds that ticket
    }

    // The account a ticket belongs to.
    public class LoginTicketAccount
    {
        public long  account_id      { get; set; }
        public bool  is_dev          { get; set; }
        public short character_limit { get; set; }
    }

    // The validation result
    public class LoginTicketCheck
    {
        public LoginTicketStatus  Status  { get; set; }
        public LoginTicketAccount Account { get; set; }
    }

    // Query row for a ticket lookup.
    public class LoginTicketRow
    {
        public long   account_id      { get; set; }
        public bool   is_dev          { get; set; }
        public short  character_limit { get; set; }
        public byte[] login_ticket    { get; set; }
    }

    // The character a ticket holder wants to play
    public class LoginCharacter
    {
        public long   character_guid { get; set; }
        public long   account_id     { get; set; }
        public bool   is_dev         { get; set; }
        public bool   is_active      { get; set; }
        public bool   is_deleted     { get; set; }
        public string name           { get; set; }
    }
}

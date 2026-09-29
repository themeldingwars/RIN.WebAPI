namespace RIN.WebAPI.Models.ClientApi
{
    public class LoginStreak
    {
        public long   id             { get; set; }
        public long   character_guid { get; set; }
        public int    streak         { get; set; }
        public string first_login    { get; set; } = string.Empty;
        public string last_updated   { get; set; } = string.Empty;

        // JSON encoded array of DailyReward, a non empty array opens the daily boost in the client
        public string loot_result    { get; set; } = "[]";
    }
}

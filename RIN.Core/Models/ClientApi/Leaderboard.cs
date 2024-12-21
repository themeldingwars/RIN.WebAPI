using RIN.WebAPI.Models.ClientApi;

namespace RIN.Core.Models.ClientApi;

public class Leaderboard : PageResults<LeaderboardResult>
{
    public     string? name { get; set; }
    public new int     page { get; set; }
}

public class LeaderboardResult
{
    public string? name           { get; set; }
    public string  rank           { get; set; }
    public string  character_name { get; set; }
    public string  current_value  { get; set; }
}

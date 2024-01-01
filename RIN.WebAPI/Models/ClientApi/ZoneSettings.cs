namespace RIN.WebAPI.Models.ClientApi
{
    public class ZoneSettingsResp
    {
        public uint Id { get; set; }
        public string Description { get; set; }
        public bool QueueingEnabled { get; set; }
        public bool ChallengeEnabled { get; set; }
        public bool SkipMatchmaking { get; set; }
        public uint TeamCount { get; set; }
        public uint MinPlayersPerTeam { get; set; }
        public uint MinPlayersAcceptPerTeam { get; set; }
        public uint MaxPlayersPerTeam { get; set; }
        public uint ChallengeMinPlayersPerTeam { get; set; }
        public uint ChallengeMaxPlayersPerTeam { get; set; }
        public string Gametype { get; set; }
        public string DisplayedName { get; set; }
        public string DisplayedGametype { get; set; }
        public uint RotationPriority { get; set; }
        public uint ZoneId { get; set; }
        public string MissionId { get; set; }
        public string SortOrder { get; set; }
        public float XpBonus { get; set; }
        public string InstanceTypePool { get; set; }
        public ZoneReward RewardWinner { get; set; }
        public ZoneReward RewardLoser { get; set; }
        public bool CertRequired { get; set; }
        public bool IsPreviewZone { get; set; }
        public string DisplayedDesc { get; set; }
        public ZoneImages Images { get; set; }
        public List<ZoneCertRequirements> CertRequirements { get; set; }
        public List<ZoneDifficultyLevels> DifficultyLevels { get; set; }
    }

    public class ZoneReward
    {
        public List<long> Items { get; set; }
        public List<long> LootSdbIds { get; set; }
    }

    public class ZoneImages
    {
        public string Thumbnail { get; set; }
        public List<string> Screenshot { get; set; }
        public string Lfg { get; set; }
    }

    public class ZoneCertRequirements
    {
        public uint Id { get; set; }
        public uint ZoneSettingId { get; set; }
        public string Presence { get; set; }
        public uint CertId { get; set; }
        public string AuthorizePosition { get; set; }
        public string DifficultyKey { get; set; }
    }

    public class ZoneDifficultyLevels
    {
        public uint Id { get; set; }
        public uint ZoneSettingId { get; set; }
        public string UiString { get; set; }
        public uint MinLevel { get; set; }
        public string DifficultyKey { get; set; }
        public uint MinPlayers { get; set; }
        public uint MinPlayersAccept { get; set; }
        public uint MaxPlayers { get; set; }
        public uint GroupMinPlayers { get; set; }
        public uint GroupMaxPlayers { get; set; }
        public uint DisplayLevel { get; set; }
        public uint MaxSuggestedLevel { get; set; }
    }
}

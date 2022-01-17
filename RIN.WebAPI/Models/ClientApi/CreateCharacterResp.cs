using System;

namespace RIN.WebAPI.Models.ClientApi
{
    public class CreateCharacterResp
    {
        public long     account_id        { get; set; }
        public long     character_guid    { get; set; }
        public DateTime created_at        { get; set; }
        public DateTime deleted_at        { get; set; }
        public int      head_accAId       { get; set; }
        public int      head_accBId       { get; set; }
        public int      head_mainId       { get; set; }
        public int      id                { get; set; }
        public bool     is_active         { get; set; }
        public bool     is_dev            { get; set; }
        public DateTime last_seen_at      { get; set; }
        public long     loadout_id        { get; set; }
        public int      max_frame_level   { get; set; }
        public string   name              { get; set; }
        public bool     needs_name_change { get; set; }
        public int      pool_id           { get; set; }
        public short    race              { get; set; }
        public long     time_played_secs  { get; set; }
        public int      title_id          { get; set; }
        public string   unique_name       { get; set; }
        public DateTime updated_at        { get; set; }
        public int      voice_setId       { get; set; }
        public string   xdata             { get; set; }
        public string   gender            { get; set; }
    }
}
using System;
using System.Collections.Generic;
using RIN.Core.Common;

namespace RIN.Core.ClientApi
{
    public class Character
    {
        public long                                character_guid    { get; set; }
        public string                              name              { get; set; }
        public string                              unique_name       { get; set; }
        public bool                                is_dev            { get; set; }
        public bool                                is_active         { get; set; }
        public DateTime                            created_at        { get; set; }
        public int                                 title_id          { get; set; }
        public int                                 time_played_secs  { get; set; }
        public bool                                needs_name_change { get; set; }
        public int                                 max_frame_level   { get; set; }
        public int                                 frame_sdb_id      { get; set; }
        public int                                 current_level     { get; set; }
        public int                                 gender            { get; set; }
        public string                              current_gender    { get; set; }
        public int                                 elite_rank        { get; set; }
        public DateTime                            last_seen_at      { get; set; }
        public CharacterBattleframeCombinedVisuals visuals           { get; set; }
        public List<GearSlot>                      gear              { get; set; }
        public long?                               expires_in        { get; set; }
        public long?                               deleted_at        { get; set; }
        public string                              race              { get; set; }
        public List<int>                           migrations        { get; set; }
    }
}
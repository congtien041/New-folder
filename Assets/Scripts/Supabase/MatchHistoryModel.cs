using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace SimpleFPS
{
    [Table("match_history")]
    public class MatchHistoryModel : BaseModel
    {
        [PrimaryKey("id", false)] // ID tự động sinh
        public string Id { get; set; }

        [Column("user_id")]
        public string UserId { get; set; }

        [Column("kills")]
        public int Kills { get; set; }

        [Column("deaths")]
        public int Deaths { get; set; }

        [Column("play_time_seconds")]
        public float PlayTimeSeconds { get; set; }

        [Column("result")]
        public string Result { get; set; }
        
        [Column("opponent_name")]
        public string OpponentName { get; set; }
    }
}
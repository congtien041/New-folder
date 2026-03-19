using Postgrest.Attributes;
using Postgrest.Models;

namespace SimpleFPS
{
    [Table("profiles")]
    public class PlayerProfile : BaseModel
    {
        // Thêm dòng [Column("id")] vào để ép Unity phải gửi ID đi
        [PrimaryKey("id", false)]
        [Column("id")]
        public string Id { get; set; }

        [Column("username")]
        public string Username { get; set; }

        [Column("device_id")]
        public string DeviceId { get; set; }

        [Column("gold")]
        public int Gold { get; set; }

        [Column("rank_points")]
        public int RankPoints { get; set; }
        [Column("current_character")]
        public string CurrentCharacter { get; set; }

        [Column("unlocked_characters")]
        public string UnlockedCharacters { get; set; }
    }
}
using Postgrest.Attributes;
using Postgrest.Models;
using System;

namespace SimpleFPS
{
    [Table("global_chat")]
    public class GlobalChatModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("sender_name")]
        public string SenderName { get; set; }

        [Column("message")]
        public string Message { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
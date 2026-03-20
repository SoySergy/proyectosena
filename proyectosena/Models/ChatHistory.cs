using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace proyectosena.Models
{
    public class ChatHistory
    {
        [Key]
        public Guid IdChatHistory { get; set; } = Guid.NewGuid();

        // ── Foreign Keys ───────────────────────────
        [Required]
        public Guid IdRequest { get; set; }

        [Required]
        public Guid IdSender { get; set; }

        // ── Columns ────────────────────────────────
        [Required, MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        public DateTime SendDate { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        // ── Navigation Properties ──────────────────
        [JsonIgnore]
        [ForeignKey("IdRequest")]
        public virtual CollectionRequest? CollectionRequest { get; set; }

        [JsonIgnore]
        [ForeignKey("IdSender")]
        public virtual User? Sender { get; set; }
    }
}

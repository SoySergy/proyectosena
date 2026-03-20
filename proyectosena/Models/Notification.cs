using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace proyectosena.Models
{
    public class Notification
    {
        [Key]
        public Guid IdNotification { get; set; } = Guid.NewGuid();

        // ── Foreign Keys (nullable) ────────────────
        public Guid? IdUser { get; set; }

        public Guid? IdRequest { get; set; }

        // ── Columns ────────────────────────────────
        [Required, MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        public DateTime CreationDate { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        // ── Navigation Properties ──────────────────
        [JsonIgnore]
        [ForeignKey("IdUser")]
        public virtual User? User { get; set; }

        [JsonIgnore]
        [ForeignKey("IdRequest")]
        public virtual CollectionRequest? CollectionRequest { get; set; }
    }
}

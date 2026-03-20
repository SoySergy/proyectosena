using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace proyectosena.Models
{
    public class History
    {
        [Key]
        public Guid IdHistory { get; set; } = Guid.NewGuid();

        // ── Foreign Keys ───────────────────────────
        [Required]
        public Guid IdRequest { get; set; }

        [Required]
        public Guid IdUser { get; set; }

        // ── Columns ────────────────────────────────
        [MaxLength(20)]
        public string? PreviousStatus { get; set; }

        [Required, MaxLength(20)]
        public string NewStatus { get; set; } = string.Empty;

        public DateTime ChangeDate { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Comment { get; set; }

        // ── Navigation Properties ──────────────────
        [JsonIgnore]
        [ForeignKey("IdRequest")]
        public virtual CollectionRequest? CollectionRequest { get; set; }

        [JsonIgnore]
        [ForeignKey("IdUser")]
        public virtual User? User { get; set; }
    }
}

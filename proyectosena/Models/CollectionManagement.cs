using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace proyectosena.Models
{
    public class CollectionManagement
    {
        [Key]
        public Guid IdManagement { get; set; } = Guid.NewGuid();

        // ── Foreign Keys ───────────────────────────
        [Required]
        public Guid IdRequest { get; set; }

        [Required]
        public Guid IdManager { get; set; }

        // ── Columns ────────────────────────────────
        [Required, MaxLength(20)]
        public string Status { get; set; } = string.Empty;

        public DateTime? StatusChangeDate { get; set; }

        public DateTime? ScheduledDate { get; set; }

        public DateTime? CompletionDate { get; set; }

        [MaxLength(200)]
        public string? ManagerObservations { get; set; }

        // ── Navigation Properties ──────────────────
        [JsonIgnore]
        [ForeignKey("IdRequest")]
        public virtual CollectionRequest? CollectionRequest { get; set; }

        [JsonIgnore]
        [ForeignKey("IdManager")]
        public virtual User? Manager { get; set; }
    }
}

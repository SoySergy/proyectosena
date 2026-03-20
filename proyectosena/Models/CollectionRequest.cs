using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace proyectosena.Models
{
    public class CollectionRequest
    {
        [Key]
        public Guid IdRequest { get; set; } = Guid.NewGuid();

        // ── Foreign Key ────────────────────────────
        [Required]
        public Guid IdUser { get; set; }

        // ── Columns ────────────────────────────────
        [Required]
        public DateTime CollectionDate { get; set; }

        [Required, MaxLength(20)]
        public string CollectionTime { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string CollectionAddress { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string ContactPhone { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string CurrentStatus { get; set; } = "Pending";

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(200)]
        public string WasteTypes { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? CitizenObservations { get; set; }

        // ── Navigation Properties ──────────────────
        [JsonIgnore]
        [ForeignKey("IdUser")]
        public virtual User? User { get; set; }

        [JsonIgnore]
        public virtual ICollection<CollectionManagement>? CollectionManagement { get; set; }

        [JsonIgnore]
        public virtual ICollection<Notification>? Notification { get; set; }

        [JsonIgnore]
        public virtual ICollection<History>? History { get; set; }

        [JsonIgnore]
        public virtual ICollection<ChatHistory>? ChatHistory { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace proyectosena.Modelos
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
        public string CurrentStatus { get; set; } = "Pendiente";

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
        public virtual ICollection<CollectionManagement>? CollectionManagements { get; set; }

        [JsonIgnore]
        public virtual ICollection<Notification>? Notifications { get; set; }

        [JsonIgnore]
        public virtual ICollection<History>? Histories { get; set; }

        [JsonIgnore]
        public virtual ICollection<ChatHistory>? ChatHistories { get; set; }
    }
}

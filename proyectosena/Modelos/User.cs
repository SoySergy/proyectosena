using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace proyectosena.Modelos
{
    public class User
    {
        [Key]
        public Guid IdUser { get; set; } = Guid.NewGuid();

        // ── Foreign Keys ───────────────────────────
        [Required]
        public Guid IdRole { get; set; }

        [Required]
        public Guid IdDocumentType { get; set; }

        // ── Columns ────────────────────────────────
        [Required, MaxLength(20)]
        public string DocumentNumber { get; set; } = string.Empty;

        [Required, MaxLength(70)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(70)]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Password { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Address { get; set; } = string.Empty;

        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        // ── Navigation Properties ──────────────────
        [JsonIgnore]
        [ForeignKey("IdRole")]
        public virtual Role? Role { get; set; }

        [JsonIgnore]
        [ForeignKey("IdDocumentType")]
        public virtual DocumentType? DocumentType { get; set; }

        [JsonIgnore]
        public virtual ICollection<CollectionRequest>? CollectionRequests { get; set; }

        [JsonIgnore]
        public virtual ICollection<CollectionManagement>? ManagedCollections { get; set; }

        [JsonIgnore]
        public virtual ICollection<Notification>? Notifications { get; set; }

        [JsonIgnore]
        public virtual ICollection<History>? Histories { get; set; }

        [JsonIgnore]
        public virtual ICollection<ChatHistory>? ChatMessages { get; set; }
    }
}

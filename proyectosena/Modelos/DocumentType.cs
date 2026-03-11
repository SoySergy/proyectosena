using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace proyectosena.Modelos
{
    public class DocumentType
    {
        [Key]
        public Guid IdDocumentType { get; set; } = Guid.NewGuid();

        [Required, MaxLength(30)]
        public string DocumentName { get; set; } = string.Empty;

        [Required, MaxLength(3)]
        public string Abbreviation { get; set; } = string.Empty;

        // ── Navigation Properties ──────────────────
        [JsonIgnore]
        public virtual ICollection<User>? Users { get; set; }
    }
}

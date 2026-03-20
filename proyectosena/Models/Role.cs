using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace proyectosena.Models
{
    public class Role
    {
        [Key]
        public Guid IdRole { get; set; } = Guid.NewGuid();

        [Required, MaxLength(50)]
        public string RoleName { get; set; } = string.Empty;

        [Required, MaxLength(250)]
        public string RoleDescription { get; set; } = string.Empty;

        // ── Navigation Properties ──────────────────
        [JsonIgnore]
        public virtual ICollection<User>? User { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace proyectosena.Models
{
    /// <summary>
    /// Solicitud de un ciudadano para que se le conceda el rol de gestor.
    /// </summary>
    /// <remarks>
    /// Es una tabla propia y no un campo en <c>User</c> a propósito: así se puede
    /// rechazar con un motivo, volver a solicitar más adelante, y queda rastro de
    /// qué administrador aprobó qué y cuándo.
    /// </remarks>
    public class ManagerApplication
    {
        [Key]
        public Guid IdApplication { get; set; } = Guid.NewGuid();

        // ── Foreign Keys ───────────────────────────
        // Quién solicita
        [Required]
        public Guid IdUser { get; set; }

        // Qué administrador la revisó. Null mientras esté pendiente.
        public Guid? IdReviewer { get; set; }

        // ── Columns ────────────────────────────────
        // Por qué quiere ser gestor. Los datos personales ya están en User;
        // esto es lo único que el administrador no puede consultar por su cuenta.
        [Required, MinLength(20), MaxLength(500)]
        public string Motivation { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Status { get; set; } = ManagerApplicationStatus.Pending;

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        // Cuándo se decidió. Null mientras esté pendiente.
        public DateTime? ReviewDate { get; set; }

        // Motivo del rechazo, para que el ciudadano sepa qué corregir
        [MaxLength(500)]
        public string? ReviewComment { get; set; }

        // ── Navigation Properties ──────────────────
        [JsonIgnore]
        [ForeignKey("IdUser")]
        public virtual User? User { get; set; }

        [JsonIgnore]
        [ForeignKey("IdReviewer")]
        public virtual User? Reviewer { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace proyectosena.DTOs.User
{
    /// <summary>
    /// Rol tal como entra y sale por la API.
    /// </summary>
    /// <remarks>
    /// Las validaciones de longitud son las mismas que tiene la entidad
    /// <c>Role</c>. Antes las ponía la entidad porque el controlador la recibía
    /// directamente en el cuerpo; al pasar al DTO tienen que viajar con él, o un
    /// nombre demasiado largo dejaría de rechazarse en el modelo y reventaría
    /// contra la base.
    ///
    /// <para>
    /// <c>IdRole</c> arranca con un Guid nuevo igual que la entidad: si el cliente
    /// no lo manda al crear, se genera aquí; si lo manda, se respeta.
    /// </para>
    /// </remarks>
    public class RoleDto
    {
        public Guid IdRole { get; set; } = Guid.NewGuid();

        [Required, MaxLength(50)]
        public string RoleName { get; set; } = string.Empty;

        [Required, MaxLength(250)]
        public string RoleDescription { get; set; } = string.Empty;
    }
}

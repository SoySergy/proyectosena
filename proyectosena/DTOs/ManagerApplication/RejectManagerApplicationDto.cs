using System.ComponentModel.DataAnnotations;

namespace proyectosena.DTOs.ManagerApplication
{
    /// <summary>
    /// El motivo por el que un administrador rechaza una solicitud.
    /// </summary>
    /// <remarks>
    /// Es obligatorio: un rechazo sin explicación deja al ciudadano sin saber
    /// qué corregir para volver a solicitar.
    /// </remarks>
    public class RejectManagerApplicationDto
    {
        [Required, MinLength(10), MaxLength(500)]
        public required string Reason { get; set; }
    }
}

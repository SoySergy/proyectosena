// =============================================
// DTO: SendMessageDto
// Usado para enviar un mensaje en el chat
// de una solicitud específica
// =============================================

using System.ComponentModel.DataAnnotations;

namespace proyectosena.DTOs.Communication
{
    public class SendMessageDto
    {
        // IdSender no viaja en el cuerpo a propósito: sale del token. Cuando venía
        // aquí, cualquiera podía firmar un mensaje con el nombre de otro.

        [Required]
        public Guid IdRequest { get; set; }

        [Required, MinLength(1), MaxLength(1000)]
        public required string Message { get; set; }
    }
}
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
        [Required]
        public Guid IdSender { get; set; }

        [Required]
        public Guid IdRequest { get; set; }

        [Required, MinLength(1), MaxLength(1000)]
        public required string Message { get; set; }
    }
}
// =============================================
// DTO: ChatMessageResponseDto
// Retorna los mensajes del chat de una solicitud
// =============================================

namespace proyectosena.DTOs.Communication
{
    public class ChatMessageResponseDto
    {
        public Guid IdChatHistory { get; set; }
        public Guid IdRequest { get; set; }
        public Guid IdSender { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderLastName { get; set; } = string.Empty;
        public string SenderRole { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime SendDate { get; set; }
        public bool IsRead { get; set; }
    }
}
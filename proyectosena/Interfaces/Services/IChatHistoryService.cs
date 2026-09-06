using proyectosena.DTOs.Communication;
using proyectosena.Models;

namespace proyectosena.Interfaces.Services
{
    /// <summary>
    /// Reglas del chat de una solicitud. Aquí vive la decisión de quién puede
    /// leer y escribir: solo el ciudadano dueño y los gestores asignados.
    /// Devuelve siempre DTOs; la entidad ChatHistory no sale de esta capa.
    /// </summary>
    public interface IChatHistoryService
    {
        // Envía un mensaje. Devuelve NotParticipant si el remitente no pertenece
        // a la solicitud, y en ese caso Message viene en null.
        Task<(ChatAccessResult Result, ChatMessageResponseDto? Message)> SendMessage(SendMessageDto dto);

        // Conversación completa de una solicitud, en orden cronológico.
        // Devuelve NotParticipant si quien consulta no pertenece a ella.
        Task<(ChatAccessResult Result, List<ChatMessageResponseDto> Messages)> GetMessagesByRequest(
            Guid idRequest, Guid idUser);

        // Mensajes de una solicitud que este usuario no ha leído
        Task<List<ChatMessageResponseDto>> GetUnreadMessages(Guid idUser, Guid idRequest);

        // Marca un mensaje como leído. False si no existe.
        Task<bool> MarkAsRead(Guid idChatHistory);
    }
}

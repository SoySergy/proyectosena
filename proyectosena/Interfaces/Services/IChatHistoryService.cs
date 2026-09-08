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
        // idSender lo pone quien llama a partir del token, no el cuerpo
        Task<(RequestAccessResult Result, ChatMessageResponseDto? Message)> SendMessage(
            SendMessageDto dto, Guid idSender);

        // Conversación completa de una solicitud, en orden cronológico.
        // Devuelve NotParticipant si quien consulta no pertenece a ella.
        Task<(RequestAccessResult Result, List<ChatMessageResponseDto> Messages)> GetMessagesByRequest(
            Guid idRequest, Guid idUser);

        // Mensajes de una solicitud que este usuario no ha leído.
        // Devuelve NotParticipant si no pertenece a ella: antes no lo comprobaba y
        // cualquiera podía leer los mensajes sin leer de una conversación ajena.
        Task<(RequestAccessResult Result, List<ChatMessageResponseDto> Messages)> GetUnreadMessages(
            Guid idUser, Guid idRequest);

        // Marca un mensaje como leído. Solo un participante de la solicitud puede
        // hacerlo: marcar como leído es tocar la conversación de otro.
        // idUser lo pone quien llama a partir del token.
        Task<RequestAccessResult> MarkAsRead(Guid idChatHistory, Guid idUser);
    }
}

using proyectosena.Models;

namespace proyectosena.Interfaces.Repositories
{
    public interface IChatHistoryRepository
    {
        // Obtiene todos los mensajes de una solicitud específica
        Task<List<ChatHistory>> GetMessagesByRequest(Guid idRequest);

        // Obtiene un mensaje específico por su ID
        Task<ChatHistory> GetMessage(Guid idChatHistory);

        // Crea un nuevo mensaje en el chat
        Task<ChatHistory> CreateMessage(ChatHistory chatHistory);


        // Marca un mensaje como leído
        Task<bool> MarkAsRead(Guid idChatHistory);

        // Obtiene los mensajes no leídos de una solicitud para un usuario específico
        Task<List<ChatHistory>> GetUnreadMessages(Guid idUser, Guid idRequest);
    }
}
using proyectosena.Models;

namespace proyectosena.Interfaces
{
    public interface IChatHistoryRepository
    {
        // Obtiene todos los mensajes del chat
        Task<List<ChatHistory>> GetMessages();

        // Obtiene todos los mensajes de una solicitud específica
        Task<List<ChatHistory>> GetMessagesByRequest(Guid idRequest);

        // Obtiene un mensaje específico por su ID
        Task<ChatHistory> GetMessage(Guid idChatHistory);

        // Crea un nuevo mensaje en el chat
        Task<ChatHistory> CreateMessage(ChatHistory chatHistory);

        // Actualiza un mensaje existente
        Task<ChatHistory> UpdateMessage(ChatHistory chatHistory);

        // Elimina un mensaje por su ID
        Task<bool> DeleteMessage(Guid idChatHistory);

        // Marca un mensaje como leído
        Task<bool> MarkAsRead(Guid idChatHistory);

        // Obtiene los mensajes no leídos de una solicitud para un usuario específico
        Task<List<ChatHistory>> GetUnreadMessages(Guid idUser, Guid idRequest);
    }
}
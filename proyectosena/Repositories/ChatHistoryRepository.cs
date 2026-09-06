using Microsoft.EntityFrameworkCore;
using proyectosena.Context;
using proyectosena.Models;
using proyectosena.Interfaces.Repositories;
using proyectosena.Interfaces.Services;

namespace proyectosena.Repositories
{
    public class ChatHistoryRepository : IChatHistoryRepository
    {
        // Contexto de la base de datos
        private readonly RecyRouteDbContext _context;

        // Constructor que recibe el contexto por inyección de dependencias
        public ChatHistoryRepository(RecyRouteDbContext context)
        {
            _context = context;
        }

        // Obtiene todos los mensajes de una solicitud específica
        // Útil para cargar el chat completo de una solicitud
        public async Task<List<ChatHistory>> GetMessagesByRequest(Guid idRequest)
        {
            return await _context.ChatHistories
                .Include(h => h.Sender)
                    .ThenInclude(s => s!.Role)
                .Where(h => h.IdRequest == idRequest)
                .OrderBy(h => h.SendDate)
                .ToListAsync();
        }

        // Obtiene un mensaje específico por su ID
        public async Task<ChatHistory> GetMessage(Guid idChatHistory)
        {
            return await _context.ChatHistories
                .Include(h => h.CollectionRequest)
                .Include(h => h.Sender)
                    .ThenInclude(s => s!.Role)
                .FirstOrDefaultAsync(h => h.IdChatHistory == idChatHistory);
        }

        // Crea un nuevo mensaje y guarda los cambios en la base de datos
        public async Task<ChatHistory> CreateMessage(ChatHistory chatHistory)
        {
            _context.ChatHistories.Add(chatHistory);
            await _context.SaveChangesAsync();
            return chatHistory;
        }


        // Elimina un mensaje por su ID
        // Retorna false si no existe

        // Marca un mensaje como leído
        // Retorna false si el mensaje no existe
        public async Task<bool> MarkAsRead(Guid idChatHistory)
        {
            var message = await _context.ChatHistories
                .FirstOrDefaultAsync(h => h.IdChatHistory == idChatHistory);
            if (message == null)
                return false;

            message.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        // Obtiene los mensajes no leídos de una solicitud que no fueron enviados por el usuario actual
        // Útil para mostrar el contador de mensajes pendientes por leer
        public async Task<List<ChatHistory>> GetUnreadMessages(Guid idUser, Guid idRequest)
        {
            return await _context.ChatHistories
                .Include(h => h.Sender)
                .Where(h => h.IdRequest == idRequest
                         && !h.IsRead
                         && h.IdSender != idUser)
                .OrderBy(h => h.SendDate)
                .ToListAsync();
        }
    }
}
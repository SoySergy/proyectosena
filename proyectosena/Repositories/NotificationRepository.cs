using Microsoft.EntityFrameworkCore;
using proyectosena.Context;
using proyectosena.Models;
using proyectosena.Interfaces;

namespace proyectosena.Repositorios
{
    public class NotificationRepository : INotificationRepository
    {
        // Contexto de la base de datos
        private readonly RecyRouteDbContext _context;

        // Constructor que recibe el contexto por inyección de dependencias
        public NotificationRepository(RecyRouteDbContext context)
        {
            _context = context;
        }

        // Obtiene todas las notificaciones incluyendo usuario y solicitud asociada
        public async Task<List<Notification>> GetNotifications()
        {
            return await _context.Notifications
                .Include(n => n.User)
                .Include(n => n.CollectionRequest)
                .ToListAsync();
        }

        // Obtiene una notificación específica por ID
        // Lanza excepción si no existe
        public async Task<Notification> GetNotification(Guid idNotification)
        {
            var notification = await _context.Notifications
                .Include(n => n.User)
                .Include(n => n.CollectionRequest)
                .FirstOrDefaultAsync(n => n.IdNotification == idNotification);

            if (notification == null)
                throw new KeyNotFoundException($"Notification with ID {idNotification} was not found.");

            return notification;
        }

        // Crea una nueva notificación y guarda los cambios en la base de datos
        public async Task<Notification> CreateNotification(Notification notification)
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        // Adds all notifications at once and saves them in a single operation
        public async Task CreateNotifications(IEnumerable<Notification> notifications)
        {
            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();
        }

        // Actualiza solo los campos modificables de una notificación existente
        // Lanza excepción si la notificación no existe
        public async Task<Notification> UpdateNotification(Notification notification)
        {
            var existing = await _context.Notifications
                .FirstOrDefaultAsync(n => n.IdNotification == notification.IdNotification);

            if (existing == null)
                throw new KeyNotFoundException($"Notification with ID {notification.IdNotification} was not found.");

            // Solo actualiza los campos permitidos
            existing.Title = notification.Title;
            existing.Message = notification.Message;
            existing.Type = notification.Type;
            existing.IsRead = notification.IsRead;

            await _context.SaveChangesAsync();
            return existing;
        }
        // Gets every notification for a user, newest first
        public async Task<List<Notification>> GetByUser(Guid idUser)
        {
            return await _context.Notifications
                .Where(n => n.IdUser == idUser)
                .OrderByDescending(n => n.CreationDate)
                .ToListAsync();
        }

        // Counts the user's unread notifications without loading them
        public async Task<int> CountUnread(Guid idUser)
        {
            return await _context.Notifications
                .CountAsync(n => n.IdUser == idUser && !n.IsRead);
        }

        // Marks all the user's unread notifications as read in a single UPDATE
        public async Task<int> MarkAllAsRead(Guid idUser)
        {
            return await _context.Notifications
                .Where(n => n.IdUser == idUser && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        }
    }
}
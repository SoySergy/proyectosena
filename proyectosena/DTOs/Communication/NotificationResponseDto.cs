// =============================================
// DTO: NotificationResponseDto
// Retorna las notificaciones de un usuario
// =============================================

namespace proyectosena.DTOs.Communication
{
    public class NotificationResponseDto
    {
        public Guid IdNotification { get; set; }
        public Guid? IdUser { get; set; }
        public Guid? IdRequest { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime CreationDate { get; set; }
        public bool IsRead { get; set; }
    }
}



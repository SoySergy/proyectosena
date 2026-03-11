// =============================================
// DTO: HistoryResponseDto
// Retorna el historial de cambios de estado
// de una solicitud
// =============================================

namespace proyectosena.DTOs.Requests
{
    public class HistoryResponseDto
    {
        public Guid IdHistory { get; set; }
        public Guid IdRequest { get; set; }
        public Guid IdUser { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? PreviousStatus { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public DateTime ChangeDate { get; set; }
        public string? Comment { get; set; }
    }
}
// =============================================
// DTO: CollectionRequestResponseDto
// Retorna el detalle completo de una solicitud
// =============================================

namespace proyectosena.DTOs.Requests
{
    public class CollectionRequestResponseDto
    {
        public Guid IdRequest { get; set; }
        public Guid IdUser { get; set; }
        public string CitizenName { get; set; } = string.Empty;
        public DateTime CollectionDate { get; set; }
        public string CollectionTime { get; set; } = string.Empty;
        public string CollectionAddress { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string WasteTypes { get; set; } = string.Empty;
        public string? CitizenObservations { get; set; }
    }
}
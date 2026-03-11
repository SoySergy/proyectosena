// =============================================
// DTO: CreateCollectionRequestDto
// Usado por el ciudadano para crear una solicitud
// =============================================
using System.ComponentModel.DataAnnotations;

namespace proyectosena.DTOs.Requests
{
    public class CreateCollectionRequestDto
    {
        [Required]
        public DateTime CollectionDate { get; set; }

        [Required, MaxLength(20)]
        public required string CollectionTime { get; set; }

        [Required, MinLength(5), MaxLength(200)]
        public required string CollectionAddress { get; set; }

        [Required, MinLength(7), MaxLength(20)]
        public required string ContactPhone { get; set; }

        [Required, MinLength(3), MaxLength(200)]
        public required string WasteTypes { get; set; }

        [MaxLength(500)]
        public string? CitizenObservations { get; set; }
    }
}

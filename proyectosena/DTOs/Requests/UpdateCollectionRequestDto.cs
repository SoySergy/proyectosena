// =============================================
// DTO: UpdateCollectionRequestDto
// Usado por el ciudadano para editar su solicitud
// Solo permite modificar mientras esté en 'Pendiente'
// =============================================

using System.ComponentModel.DataAnnotations;

namespace proyectosena.DTOs.Requests
{
    public class UpdateCollectionRequestDto
    {
        [Required]
        public Guid IdRequest { get; set; }

        public DateTime? CollectionDate { get; set; }

        [MaxLength(20)]
        public string? CollectionTime { get; set; }

        [MinLength(5), MaxLength(200)]
        public string? CollectionAddress { get; set; }

        [MinLength(7), MaxLength(20)]
        public string? ContactPhone { get; set; }

        [MinLength(3), MaxLength(200)]
        public string? WasteTypes { get; set; }

        [MaxLength(500)]
        public string? CitizenObservations { get; set; }
    }
}
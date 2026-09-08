using proyectosena.DTOs.Collection;

namespace proyectosena.Interfaces.Services
{
    /// <summary>
    /// Consulta de quién gestiona una solicitud de recolección.
    /// </summary>
    /// <remarks>
    /// Las filas de gestión las crea <c>AssignmentService</c> cuando un gestor
    /// acepta una solicitud o un administrador la reasigna. Aquí solo se leen.
    /// </remarks>
    public interface ICollectionManagementService
    {
        // Gestión vigente de una solicitud. Null si todavía nadie la ha tomado:
        // una solicitud pendiente no tiene gestor, y eso no es un error.
        Task<CollectionManagementResponseDto?> GetByRequest(Guid idRequest);
    }
}

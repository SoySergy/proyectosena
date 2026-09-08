using proyectosena.DTOs.User;
using proyectosena.Models;

namespace proyectosena.Interfaces.Services
{
    /// <summary>
    /// Operaciones que solo ejecuta un administrador: alta de gestores,
    /// estadísticas del panel y reasignación de solicitudes.
    /// </summary>
    public interface IAdminService
    {
        // Crea la cuenta del gestor y le envía el código para que ponga su
        // propia contraseña. Los datos del gestor creado vienen en null salvo
        // que el resultado sea Success.
        Task<(CreateManagerResult Result, Guid IdUser, string Email, int ExpiresInMinutes)> CreateManager(CreateManagerDto dto);

        // Números agregados para el panel del administrador
        Task<DashboardStatsDto> GetDashboardStats();

        // Pasa una solicitud activa a otro gestor. El mensaje explica el motivo
        // cuando no se pudo.
        Task<(bool Success, string Message)> ReassignRequest(Guid idRequest, Guid idNewManager, Guid idAdmin);
    }
}

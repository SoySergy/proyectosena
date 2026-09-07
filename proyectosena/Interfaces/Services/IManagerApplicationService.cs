using proyectosena.DTOs.Common;
using proyectosena.DTOs.ManagerApplication;
using proyectosena.Models;

namespace proyectosena.Interfaces.Services
{
    /// <summary>
    /// Solicitudes de ciudadanos que quieren ser gestores.
    /// </summary>
    /// <remarks>
    /// Todo el mundo se registra como ciudadano. Este es el único camino para
    /// llegar a gestor desde fuera; el administrador también puede dar de alta
    /// empleados directamente con <c>CreateManager</c>, sin esperar solicitud.
    /// </remarks>
    public interface IManagerApplicationService
    {
        // ── Lado del ciudadano ──────────────────────────────────────────

        // idUser lo pone quien llama a partir del token.
        // Application viene en null salvo que el resultado sea Success.
        Task<(ManagerApplicationResult Result, ManagerApplicationResponseDto? Application)> Apply(
            CreateManagerApplicationDto dto, Guid idUser);

        // La solicitud más reciente del ciudadano. Null si nunca ha solicitado.
        Task<ManagerApplicationResponseDto?> GetMine(Guid idUser);

        // ── Lado del administrador ──────────────────────────────────────

        // Bandeja de solicitudes por revisar, la más antigua primero
        Task<PagedResult<ManagerApplicationResponseDto>> GetPending(int page, int pageSize);

        // Concede el rol de gestor al solicitante y le avisa.
        // idReviewer es el administrador que decide, sacado de su token.
        Task<(ManagerApplicationDecisionResult Result, ManagerApplicationResponseDto? Application)> Approve(
            Guid idApplication, Guid idReviewer);

        // Rechaza con un motivo y le avisa. El ciudadano puede volver a solicitar.
        Task<(ManagerApplicationDecisionResult Result, ManagerApplicationResponseDto? Application)> Reject(
            Guid idApplication, Guid idReviewer, string reason);
    }
}

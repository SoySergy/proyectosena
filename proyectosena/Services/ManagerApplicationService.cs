using proyectosena.DTOs.Common;
using proyectosena.DTOs.ManagerApplication;
using proyectosena.Interfaces.Repositories;
using proyectosena.Interfaces.Services;
using proyectosena.Models;

namespace proyectosena.Services
{
    public class ManagerApplicationService : IManagerApplicationService
    {
        private readonly IManagerApplicationRepository _applicationRepository;
        private readonly INotificationRepository _notificationRepository;

        public ManagerApplicationService(
            IManagerApplicationRepository applicationRepository,
            INotificationRepository notificationRepository)
        {
            _applicationRepository = applicationRepository;
            _notificationRepository = notificationRepository;
        }

        // ── Lado del ciudadano ──────────────────────────────────────────

        public async Task<(ManagerApplicationResult Result, ManagerApplicationResponseDto? Application)> Apply(
            CreateManagerApplicationDto dto, Guid idUser)
        {
            // Una sola solicitud a la vez: si no, la bandeja del administrador se
            // llena de copias de la misma persona.
            if (await _applicationRepository.HasPending(idUser))
                return (ManagerApplicationResult.AlreadyPending, null);

            // Si ya se la aprobaron antes, ya es gestor y no tiene qué pedir.
            var ultima = await _applicationRepository.GetLatestByUser(idUser);
            if (ultima?.Status == ManagerApplicationStatus.Approved)
                return (ManagerApplicationResult.AlreadyApproved, null);

            // Un rechazo anterior no bloquea: se puede volver a solicitar.
            var application = new ManagerApplication
            {
                IdUser = idUser,
                Motivation = dto.Motivation,
                Status = ManagerApplicationStatus.Pending,
                RequestDate = DateTime.UtcNow
            };

            var created = await _applicationRepository.Create(application);

            return (ManagerApplicationResult.Success, MapToDto(created));
        }

        public async Task<ManagerApplicationResponseDto?> GetMine(Guid idUser)
        {
            var application = await _applicationRepository.GetLatestByUser(idUser);
            return application == null ? null : MapToDto(application);
        }

        // ── Lado del administrador ──────────────────────────────────────

        public async Task<PagedResult<ManagerApplicationResponseDto>> GetPending(int page, int pageSize)
        {
            (page, pageSize) = PagedResult<ManagerApplicationResponseDto>.Normalize(page, pageSize);

            var (items, total) = await _applicationRepository.GetPending(page, pageSize);

            return PagedResult<ManagerApplicationResponseDto>.Create(
                items.Select(MapToDto).ToList(), page, pageSize, total);
        }

        public async Task<(ManagerApplicationDecisionResult Result, ManagerApplicationResponseDto? Application)> Approve(
            Guid idApplication, Guid idReviewer)
        {
            var (result, application) = await BuscarPendiente(idApplication);
            if (result != ManagerApplicationDecisionResult.Success)
                return (result, null);

            application!.Status = ManagerApplicationStatus.Approved;
            application.IdReviewer = idReviewer;
            application.ReviewDate = DateTime.UtcNow;

            // El servicio decide qué rol se concede; el repositorio solo persiste
            // los dos cambios juntos.
            var aprobada = await _applicationRepository.ApproveAndGrantRole(
                application, SeedIds.Roles.Manager);

            await Avisar(aprobada.IdUser,
                "Manager Application Approved",
                "Your application to become a manager was approved. Sign in again to see your new options.",
                "Success");

            return (ManagerApplicationDecisionResult.Success, MapToDto(aprobada));
        }

        public async Task<(ManagerApplicationDecisionResult Result, ManagerApplicationResponseDto? Application)> Reject(
            Guid idApplication, Guid idReviewer, string reason)
        {
            var (result, application) = await BuscarPendiente(idApplication);
            if (result != ManagerApplicationDecisionResult.Success)
                return (result, null);

            application!.Status = ManagerApplicationStatus.Rejected;
            application.IdReviewer = idReviewer;
            application.ReviewDate = DateTime.UtcNow;
            application.ReviewComment = reason;

            var rechazada = await _applicationRepository.SaveDecision(application);

            // Se le manda el motivo para que sepa qué corregir antes de volver a
            // solicitar; un rechazo no cierra la puerta.
            await Avisar(rechazada.IdUser,
                "Manager Application Rejected",
                $"Your application to become a manager was not approved. Reason: {reason}",
                "Warning");

            return (ManagerApplicationDecisionResult.Success, MapToDto(rechazada));
        }

        // ── Privados ────────────────────────────────────────────────────

        // Una solicitud solo se decide una vez, y solo si está pendiente
        private async Task<(ManagerApplicationDecisionResult Result, ManagerApplication? Application)>
            BuscarPendiente(Guid idApplication)
        {
            var application = await _applicationRepository.GetById(idApplication);

            if (application == null)
                return (ManagerApplicationDecisionResult.ApplicationNotFound, null);

            if (application.Status != ManagerApplicationStatus.Pending)
                return (ManagerApplicationDecisionResult.AlreadyDecided, null);

            return (ManagerApplicationDecisionResult.Success, application);
        }

        private Task Avisar(Guid idUser, string titulo, string mensaje, string tipo)
            => _notificationRepository.CreateNotification(new Notification
            {
                IdUser = idUser,

                // Sin IdRequest: esta notificación no habla de una solicitud de
                // recolección sino de la cuenta de la persona.
                IdRequest = null,
                Title = titulo,
                Message = mensaje,
                Type = tipo,
                IsRead = false,
                CreationDate = DateTime.UtcNow
            });

        // ── Mapeo ───────────────────────────────────────────────────────
        // Aplana al solicitante y al revisor: el cliente recibe nombres, nunca
        // la entidad User.
        private static ManagerApplicationResponseDto MapToDto(ManagerApplication a) => new()
        {
            IdApplication = a.IdApplication,
            IdUser = a.IdUser,
            ApplicantName = a.User != null ? $"{a.User.Name} {a.User.LastName}" : string.Empty,
            ApplicantEmail = a.User?.Email ?? string.Empty,
            ApplicantDocument = a.User?.DocumentNumber ?? string.Empty,
            ApplicantPhone = a.User?.PhoneNumber ?? string.Empty,
            Motivation = a.Motivation,
            Status = a.Status,
            RequestDate = a.RequestDate,
            ReviewerName = a.Reviewer != null ? $"{a.Reviewer.Name} {a.Reviewer.LastName}" : null,
            ReviewDate = a.ReviewDate,
            ReviewComment = a.ReviewComment
        };
    }
}

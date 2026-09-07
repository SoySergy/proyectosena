using Microsoft.EntityFrameworkCore;
using proyectosena.DTOs.User;
using proyectosena.Extensions;
using proyectosena.Interfaces.Repositories;
using proyectosena.Interfaces.Services;
using proyectosena.Models;
using System.Security.Cryptography;

namespace proyectosena.Services
{
    public class AdminService : IAdminService
    {
        // El código de invitación dura más que uno de recuperación: el gestor
        // puede no estar frente a su correo cuando se le crea la cuenta.
        private const int InvitationExpiryMinutes = 60;

        // Ventana de actividad que muestra el panel
        private const int ActivityWindowDays = 30;

        private readonly IUserLookupRepository _userLookup;
        private readonly IUserDirectoryRepository _userDirectory;
        private readonly IUserWriteRepository _userWrite;
        private readonly ICollectionRequestRepository _requestRepository;
        private readonly IAssignmentService _assignmentService;
        private readonly IEmailService _emailService;
        private readonly IPasswordResetService _resetService;

        public AdminService(
            IUserLookupRepository userLookup,
            IUserDirectoryRepository userDirectory,
            IUserWriteRepository userWrite,
            ICollectionRequestRepository requestRepository,
            IAssignmentService assignmentService,
            IEmailService emailService,
            IPasswordResetService resetService)
        {
            _userLookup = userLookup;
            _userDirectory = userDirectory;
            _userWrite = userWrite;
            _requestRepository = requestRepository;
            _assignmentService = assignmentService;
            _emailService = emailService;
            _resetService = resetService;
        }

        public async Task<(CreateManagerResult Result, Guid IdUser, string Email, int ExpiresInMinutes)>
            CreateManager(CreateManagerDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            // El correo debe estar libre incluso entre usuarios dados de baja:
            // al índice único no le importa si una cuenta está activa.
            var existingEmail = await _userLookup.GetUserByEmail(email);
            if (existingEmail != null)
                return (CreateManagerResult.EmailAlreadyUsed, Guid.Empty, string.Empty, 0);

            var existingDoc = await _userLookup.GetUserByDocument(dto.DocumentNumber, dto.IdDocumentType);
            if (existingDoc != null)
                return (CreateManagerResult.DocumentAlreadyUsed, Guid.Empty, string.Empty, 0);

            // Contraseña aleatoria que nadie conoce: la cuenta no sirve hasta
            // que el gestor ponga la suya con el código del correo.
            var unusablePassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            var manager = new User
            {
                IdUser = Guid.NewGuid(),
                IdRole = SeedIds.Roles.Manager,
                IdDocumentType = dto.IdDocumentType,
                DocumentNumber = dto.DocumentNumber,
                Name = dto.Name,
                LastName = dto.LastName,
                Email = email,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,
                Password = BCrypt.Net.BCrypt.HashPassword(unusablePassword),
                RegistrationDate = DateTime.UtcNow,
                IsActive = true
            };

            User created;
            try
            {
                created = await _userWrite.CreateUser(manager);
            }
            catch (DbUpdateException ex) when (ex.IsDuplicateKey())
            {
                return (CreateManagerResult.DuplicateOnSave, Guid.Empty, string.Empty, 0);
            }

            // Mismo mecanismo de código que el flujo de recuperación, con más vida
            var code = _resetService.GenerateAndStoreCode(email, InvitationExpiryMinutes);
            await _emailService.SendManagerInvitationAsync(email, dto.Name, code, InvitationExpiryMinutes);

            return (CreateManagerResult.Success, created.IdUser, created.Email, InvitationExpiryMinutes);
        }

        public async Task<DashboardStatsDto> GetDashboardStats()
        {
            var counts = await _requestRepository.GetStatusCounts();

            return new DashboardStatsDto
            {
                // Un estado sin solicitudes simplemente no aparece en el diccionario
                Pending = counts.GetValueOrDefault(CollectionRequestStatus.Pending),
                Assigned = counts.GetValueOrDefault(CollectionRequestStatus.Assigned),
                InProgress = counts.GetValueOrDefault(CollectionRequestStatus.InProgress),
                Completed = counts.GetValueOrDefault(CollectionRequestStatus.Completed),
                Rejected = counts.GetValueOrDefault(CollectionRequestStatus.Rejected),
                TotalRequests = counts.Values.Sum(),
                RequestsLast30Days = await _requestRepository.CountSince(DateTime.UtcNow.AddDays(-ActivityWindowDays)),
                ActiveManagers = await _userDirectory.CountByRole("Manager"),
                ActiveCitizens = await _userDirectory.CountByRole("Citizen")
            };
        }

        public Task<(bool Success, string Message)> ReassignRequest(Guid idRequest, Guid idNewManager, Guid idAdmin)
            => _assignmentService.ReassignRequestAsync(idRequest, idNewManager, idAdmin);
    }
}

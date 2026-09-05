using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using proyectosena.DTOs.User;
using proyectosena.Interfaces;
using proyectosena.Models;
using proyectosena.Repositories.Interfaces;
using System.Security.Cryptography;

namespace proyectosena.Controllers
{
    // Administration operations. Every endpoint here is Administrator-only.
    [Authorize(Policy = "AdminOnly")]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        // Manager role is seeded in the database, so its id is stable
        private static readonly Guid ManagerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        // Invitation codes last longer than a password reset: the manager may not
        // be at their inbox when the account is created.
        private const int InvitationExpiryMinutes = 60;

        private readonly IUserRepository _userRepository;
        private readonly ICollectionRequestRepository _requestRepository;
        private readonly IAssignmentService _assignmentService;
        private readonly IEmailService _emailService;
        private readonly IPasswordResetService _resetService;

        public AdminController(
            IUserRepository userRepository,
            ICollectionRequestRepository requestRepository,
            IAssignmentService assignmentService,
            IEmailService emailService,
            IPasswordResetService resetService)
        {
            _userRepository = userRepository;
            _requestRepository = requestRepository;
            _assignmentService = assignmentService;
            _emailService = emailService;
            _resetService = resetService;
        }

        // -------------------- POST: api/admin/CreateManager --------------------
        // Registers a manager. The password is never chosen here: the account is
        // created with an unusable random one and the manager sets their own
        // through the code emailed to them.
        [HttpPost("CreateManager")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateManager([FromBody] CreateManagerDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest("Manager data cannot be null.");

                var email = dto.Email.Trim().ToLowerInvariant();

                // The email must be free, including among deactivated users:
                // the unique index does not care whether an account is active.
                var existingEmail = await _userRepository.GetUserByEmail(email);
                if (existingEmail != null)
                    return BadRequest("Ya existe un usuario con este correo.");

                var existingDoc = await _userRepository.GetUserByDocument(dto.DocumentNumber, dto.IdDocumentType);
                if (existingDoc != null)
                    return BadRequest("El número de documento ya se encuentra registrado con este tipo de documento.");

                // Random password nobody knows, so the account cannot be used until activated
                var unusablePassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

                var manager = new User
                {
                    IdUser = Guid.NewGuid(),
                    IdRole = ManagerRoleId,
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

                var created = await _userRepository.CreateUser(manager);

                // Same code mechanism as the password reset flow, just a longer life
                var code = _resetService.GenerateAndStoreCode(email, InvitationExpiryMinutes);
                await _emailService.SendManagerInvitationAsync(email, dto.Name, code, InvitationExpiryMinutes);

                return Ok(new
                {
                    Message = "Gestor creado. Se envió un código de activación a su correo.",
                    IdUser = created.IdUser,
                    Email = created.Email,
                    ExpiresInMinutes = InvitationExpiryMinutes
                });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
                when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx
                      && (sqlEx.Number == 2627 || sqlEx.Number == 2601))
            {
                return BadRequest("El correo o el documento ya se encuentran registrados.");
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Ocurrió un error al crear el gestor. Por favor intente más tarde.");
            }
        }

        // -------------------- GET: api/admin/GetDashboardStats --------------------
        // Aggregated numbers for the administrator dashboard.
        [HttpGet("GetDashboardStats")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var counts = await _requestRepository.GetStatusCounts();

                var stats = new DashboardStatsDto
                {
                    // A status with no requests is simply missing from the dictionary
                    Pending = counts.GetValueOrDefault(CollectionRequestStatus.Pending),
                    Assigned = counts.GetValueOrDefault(CollectionRequestStatus.Assigned),
                    InProgress = counts.GetValueOrDefault(CollectionRequestStatus.InProgress),
                    Completed = counts.GetValueOrDefault(CollectionRequestStatus.Completed),
                    Rejected = counts.GetValueOrDefault(CollectionRequestStatus.Rejected),
                    TotalRequests = counts.Values.Sum(),
                    RequestsLast30Days = await _requestRepository.CountSince(DateTime.UtcNow.AddDays(-30)),
                    ActiveManagers = await _userRepository.CountByRole("Manager"),
                    ActiveCitizens = await _userRepository.CountByRole("Citizen")
                };

                return Ok(stats);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Error retrieving the dashboard statistics.");
            }
        }

        // -------------------- PATCH: api/admin/ReassignRequest --------------------
        // Moves an active request to a different manager, for example when the
        // current one is unavailable.
        [HttpPatch("ReassignRequest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ReassignRequest(Guid idRequest, Guid idNewManager, Guid idAdmin)
        {
            try
            {
                var (success, message) = await _assignmentService
                    .ReassignRequestAsync(idRequest, idNewManager, idAdmin);

                if (!success)
                    return BadRequest(message);

                return Ok(new
                {
                    Message = message,
                    IdRequest = idRequest,
                    IdNewManager = idNewManager,
                    ReassignedAt = DateTime.UtcNow
                });
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Error reassigning the request.");
            }
        }
    }
}

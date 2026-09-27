using proyectosena.DTOs.Common;
using proyectosena.DTOs.User;
using proyectosena.Interfaces.Repositories;
using proyectosena.Interfaces.Services;
using proyectosena.Mappers;
using proyectosena.Models;

namespace proyectosena.Services
{
    public class UserService : IUserService
    {
        private readonly IUserLookupRepository _userLookup;
        private readonly IUserDirectoryRepository _userDirectory;
        private readonly IUserWriteRepository _userWrite;

        // Para que dar de baja o cambiar la contraseña cierre de golpe
        // cualquier sesión abierta de esa persona, no solo la que hace la
        // petición
        private readonly IRevokedTokenService _revokedTokens;

        public UserService(
            IUserLookupRepository userLookup,
            IUserDirectoryRepository userDirectory,
            IUserWriteRepository userWrite,
            IRevokedTokenService revokedTokens)
        {
            _userLookup = userLookup;
            _userDirectory = userDirectory;
            _userWrite = userWrite;
            _revokedTokens = revokedTokens;
        }

        public async Task<PagedResult<UserInfoDto>> GetUsers(UserFilterDto filter)
        {
            var (page, pageSize) = PagedResult<UserInfoDto>.Normalize(filter.Page, filter.PageSize);

            var (items, total) = await _userDirectory.GetUsers(ToQuery(filter), page, pageSize);

            return PagedResult<UserInfoDto>.Create(
                items.Select(u => u.ToInfoDto()).ToList(), page, pageSize, total);
        }

        // Traduce lo que pidió el cliente a lo que entiende la base. El rol se
        // guarda con la primera letra en mayúscula, así que se normaliza aquí y
        // no en cada consulta.
        private static UserQuery ToQuery(UserFilterDto filter) => new(
            RoleNames.All.FirstOrDefault(r => string.Equals(r, filter.Role, StringComparison.OrdinalIgnoreCase)),
            filter.IsActive,
            filter.EmailVerified,
            string.IsNullOrWhiteSpace(filter.Search) ? null : filter.Search.Trim(),
            filter.OrdersByRegistrationDate,
            filter.Ascending);

        public async Task<UserInfoDto?> GetById(Guid idUser)
        {
            var user = await _userLookup.GetUser(idUser);
            return user == null ? null : user.ToInfoDto();
        }

        public async Task<UserInfoDto?> GetByEmail(string email)
        {
            var user = await _userLookup.GetUserByEmail(email);
            return user == null ? null : user.ToInfoDto();
        }

        public async Task<UserInfoDto?> GetByDocument(string documentNumber, Guid idDocumentType)
        {
            var user = await _userLookup.GetUserByDocument(documentNumber, idDocumentType);
            return user == null ? null : user.ToInfoDto();
        }

        public async Task<List<UserInfoDto>> GetByRole(string roleName)
        {
            var users = await _userDirectory.GetByRoleNameAsync(roleName);
            return users.Select(u => u.ToInfoDto()).ToList();
        }

        public async Task<(UserUpdateResult Result, UserInfoDto? User)> UpdateUser(Guid idUser, UpdateUserDto dto)
        {
            var user = await _userLookup.GetUser(idUser);
            if (user == null)
                return (UserUpdateResult.UserNotFound, null);

            // Solo se tocan los campos que vengan con valor
            if (dto.Name != null) user.Name = dto.Name;
            if (dto.LastName != null) user.LastName = dto.LastName;
            if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;
            if (dto.Address != null) user.Address = dto.Address;

            // Cambiar la contraseña exige demostrar que se conoce la actual
            if (!string.IsNullOrEmpty(dto.NewPassword))
            {
                if (string.IsNullOrEmpty(dto.CurrentPassword))
                    return (UserUpdateResult.CurrentPasswordRequired, null);

                // BCrypt hashea el intento y compara los hashes; nunca descifra
                if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.Password))
                    return (UserUpdateResult.CurrentPasswordIncorrect, null);

                user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            }

            var updated = await _userWrite.UpdateUser(user);

            // La contraseña nueva no sirve de nada si una sesión robada con la
            // vieja sigue funcionando. Incluye a la sesión que hizo este mismo
            // cambio: vuelve a entrar con la contraseña que acaba de poner.
            if (!string.IsNullOrEmpty(dto.NewPassword))
                await _revokedTokens.RevokeAllForUser(idUser);

            return (UserUpdateResult.Success, updated.ToInfoDto());
        }

        public async Task<UserDeactivationResult> Deactivate(Guid idUser)
        {
            // El freno del último administrador y la baja misma van juntos,
            // bajo un candado, en DeactivateWithLastAdminGuard: separados
            // (como antes) dos bajas simultáneas de administradores distintos
            // podían leer "quedan 2" antes de que ninguna escribiera, y las
            // dos pasaban el freno a la vez, dejando el sistema en cero (WA-16).
            var result = await _userWrite.DeactivateWithLastAdminGuard(idUser, RoleNames.Administrator);
            if (result != UserDeactivationResult.Success)
                return result;

            // De nada sirve la baja si el token que ya tenía sigue sirviendo
            // los minutos que le quedaban de vida.
            await _revokedTokens.RevokeAllForUser(idUser);

            return UserDeactivationResult.Success;
        }
    }
}

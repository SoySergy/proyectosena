using proyectosena.DTOs.Common;
using proyectosena.DTOs.User;
using proyectosena.Interfaces.Repositories;
using proyectosena.Interfaces.Services;
using proyectosena.Models;

namespace proyectosena.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<PagedResult<UserInfoDto>> GetUsers(int page, int pageSize)
        {
            (page, pageSize) = PagedResult<UserInfoDto>.Normalize(page, pageSize);

            var (items, total) = await _userRepository.GetUsers(page, pageSize);

            return PagedResult<UserInfoDto>.Create(
                items.Select(MapToDto).ToList(), page, pageSize, total);
        }

        public async Task<UserInfoDto?> GetById(Guid idUser)
        {
            var user = await _userRepository.GetUser(idUser);
            return user == null ? null : MapToDto(user);
        }

        public async Task<UserInfoDto?> GetByEmail(string email)
        {
            var user = await _userRepository.GetUserByEmail(email);
            return user == null ? null : MapToDto(user);
        }

        public async Task<UserInfoDto?> GetByDocument(string documentNumber, Guid idDocumentType)
        {
            var user = await _userRepository.GetUserByDocument(documentNumber, idDocumentType);
            return user == null ? null : MapToDto(user);
        }

        public async Task<List<UserInfoDto>> GetByRole(string roleName)
        {
            var users = await _userRepository.GetByRoleNameAsync(roleName);
            return users.Select(MapToDto).ToList();
        }

        public async Task<(UserUpdateResult Result, UserInfoDto? User)> UpdateUser(Guid idUser, UpdateUserDto dto)
        {
            var user = await _userRepository.GetUser(idUser);
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

            var updated = await _userRepository.UpdateUser(user);

            return (UserUpdateResult.Success, MapToDto(updated));
        }

        public Task<bool> Deactivate(Guid idUser)
            => _userRepository.DeleteUser(idUser);

        // ── Mapeo privado ───────────────────────────────────────────────
        // Deja fuera Password: el hash no sale nunca de esta capa
        private static UserInfoDto MapToDto(User user) => new()
        {
            IdUser = user.IdUser,
            IdRole = user.IdRole,
            RoleName = user.Role?.RoleName ?? string.Empty,
            IdDocumentType = user.IdDocumentType,
            DocumentTypeName = user.DocumentType?.DocumentName ?? string.Empty,
            DocumentNumber = user.DocumentNumber,
            Name = user.Name,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            RegistrationDate = user.RegistrationDate
        };
    }
}

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

        public UserService(
            IUserLookupRepository userLookup,
            IUserDirectoryRepository userDirectory,
            IUserWriteRepository userWrite)
        {
            _userLookup = userLookup;
            _userDirectory = userDirectory;
            _userWrite = userWrite;
        }

        public async Task<PagedResult<UserInfoDto>> GetUsers(int page, int pageSize)
        {
            (page, pageSize) = PagedResult<UserInfoDto>.Normalize(page, pageSize);

            var (items, total) = await _userDirectory.GetUsers(page, pageSize);

            return PagedResult<UserInfoDto>.Create(
                items.Select(u => u.ToInfoDto()).ToList(), page, pageSize, total);
        }

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

            return (UserUpdateResult.Success, updated.ToInfoDto());
        }

        public Task<bool> Deactivate(Guid idUser)
            => _userWrite.DeleteUser(idUser);
    }
}

using proyectosena.DTOs.Common;
using proyectosena.DTOs.User;
using proyectosena.Models;

namespace proyectosena.Interfaces.Services
{
    /// <summary>
    /// Reglas de negocio de los usuarios: consulta de perfiles, actualización
    /// de datos y cambio de contraseña. Devuelve siempre DTOs — la entidad User
    /// nunca sale de esta capa, y con ella nunca sale el hash de la contraseña.
    /// </summary>
    public interface IUserService
    {
        // Página de usuarios activos
        Task<PagedResult<UserInfoDto>> GetUsers(int page, int pageSize);

        // Búsquedas puntuales. Devuelven null si no hay coincidencia.
        Task<UserInfoDto?> GetById(Guid idUser);
        Task<UserInfoDto?> GetByEmail(string email);
        Task<UserInfoDto?> GetByDocument(string documentNumber, Guid idDocumentType);

        // Usuarios activos de un rol
        Task<List<UserInfoDto>> GetByRole(string roleName);

        // Actualiza perfil y, opcionalmente, la contraseña. El resultado dice
        // por qué falló: usuario inexistente, falta la contraseña actual, o no
        // coincide. User viene en null salvo que el resultado sea Success.
        Task<(UserUpdateResult Result, UserInfoDto? User)> UpdateUser(Guid idUser, UpdateUserDto dto);

        // Baja lógica: la fila se conserva para auditoría. False si no existe.
        Task<bool> Deactivate(Guid idUser);
    }
}

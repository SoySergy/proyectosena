using proyectosena.DTOs.User;

namespace proyectosena.Interfaces.Services
{
    /// <summary>
    /// Administración del catálogo de roles.
    /// </summary>
    /// <remarks>
    /// Devuelve DTOs, no la entidad: <c>Role</c> arrastra la colección de usuarios
    /// que lo tienen asignado, y eso no tiene por qué asomarse a la API.
    /// </remarks>
    public interface IRoleService
    {
        // Ordenados por nombre
        Task<List<RoleDto>> GetAll();

        // Null si no existe
        Task<RoleDto?> GetById(Guid idRole);

        Task<RoleDto> Create(RoleDto dto);

        Task<RoleDto> Update(RoleDto dto);

        // False si no existe
        Task<bool> Delete(Guid idRole);
    }
}

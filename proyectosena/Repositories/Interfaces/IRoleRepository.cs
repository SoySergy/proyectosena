using proyectosena.Models;

namespace proyectosena.Interfaces
{
    public interface IRoleRepository
    {
        // Obtiene todos los roles
        Task<List<Role>> GetRoles();

        // Obtiene un rol por su ID
        Task<Role?> GetRole(Guid idRole);

        // Crea un nuevo rol
        Task<Role> CreateRole(Role role);

        // Actualiza un rol existente
        Task<Role> UpdateRole(Role role);

        // Elimina un rol por su ID
        Task<bool> DeleteRole(Guid idRole);
    }
}
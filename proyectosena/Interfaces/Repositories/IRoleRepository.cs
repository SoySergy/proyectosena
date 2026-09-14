using proyectosena.Models;

namespace proyectosena.Interfaces.Repositories
{
    public interface IRoleRepository
    {
        // Obtiene todos los roles
        Task<List<Role>> GetRoles();

        // Obtiene un rol por su ID
        Task<Role?> GetRole(Guid idRole);

        // Crea un nuevo rol
        Task<Role> CreateRole(Role role);

        // Actualiza un rol existente. NotFound si el id no existe (WA-06):
        // antes el UPDATE de EF Core sobre una fila inexistente reventaba con
        // DbUpdateConcurrencyException, y eso llegaba como 500.
        Task<(CatalogMutationResult Result, Role? Role)> UpdateRole(Role role);

        // Elimina un rol por su ID. InUse si algún usuario todavía lo tiene
        // asignado (WA-06): antes la clave foránea reventaba como 500.
        Task<CatalogMutationResult> DeleteRole(Guid idRole);
    }
}
using Microsoft.EntityFrameworkCore;
using proyectosena.Context;
using proyectosena.Extensions;
using proyectosena.Interfaces.Repositories;
using proyectosena.Interfaces.Services;
using proyectosena.Models;

namespace proyectosena.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        // Contexto de la base de datos
        private readonly RecyRouteDbContext _context;

        // Constructor que recibe el contexto por inyección de dependencias
        public RoleRepository(RecyRouteDbContext context)
        {
            _context = context;
        }

        // Obtiene todos los roles registrados en la base de datos
        public async Task<List<Role>> GetRoles()
        {
            return await _context.Roles
                                 .OrderBy(r => r.RoleName)
                                 .ToListAsync();
        }

        // Obtiene un rol específico por su ID
        public async Task<Role?> GetRole(Guid idRole)
        {
            return await _context.Roles
                                 .FirstOrDefaultAsync(r => r.IdRole == idRole);
        }

        // Crea un nuevo rol y guarda los cambios en la base de datos
        public async Task<Role> CreateRole(Role role)
        {
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
            return role;
        }

        // Actualiza un rol existente y guarda los cambios en la base de datos
        public async Task<(CatalogMutationResult Result, Role? Role)> UpdateRole(Role role)
        {
            var existing = await _context.Roles.FirstOrDefaultAsync(r => r.IdRole == role.IdRole);
            if (existing == null)
                return (CatalogMutationResult.NotFound, null);

            existing.RoleName = role.RoleName;
            existing.RoleDescription = role.RoleDescription;
            await _context.SaveChangesAsync();
            return (CatalogMutationResult.Success, existing);
        }

        // Elimina un rol por su ID
        public async Task<CatalogMutationResult> DeleteRole(Guid idRole)
        {
            var role = await _context.Roles
                                     .FirstOrDefaultAsync(r => r.IdRole == idRole);
            if (role == null)
                return CatalogMutationResult.NotFound;

            _context.Roles.Remove(role);
            try
            {
                await _context.SaveChangesAsync();
                return CatalogMutationResult.Success;
            }
            catch (DbUpdateException ex) when (ex.IsForeignKeyViolation())
            {
                return CatalogMutationResult.InUse;
            }
        }
    }
}
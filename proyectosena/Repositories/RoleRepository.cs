using Microsoft.EntityFrameworkCore;
using proyectosena.Context;
using proyectosena.Interfaces;
using proyectosena.Models;

namespace proyectosena.Repositorios
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
            return await _context.Roles.ToListAsync();
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
        public async Task<Role> UpdateRole(Role role)
        {
            _context.Roles.Update(role);
            await _context.SaveChangesAsync();
            return role;
        }

        // Elimina un rol por su ID, retorna false si no existe
        public async Task<bool> DeleteRole(Guid idRole)
        {
            var role = await _context.Roles
                                     .FirstOrDefaultAsync(r => r.IdRole == idRole);
            if (role == null)
                return false;

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
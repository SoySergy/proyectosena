using Microsoft.EntityFrameworkCore;
using proyectosena.Context;
using proyectosena.Interfaces;
using proyectosena.Models;

namespace proyectosena.Repositorios
{
    public class UserRepository : IUserRepository
    {
        // Contexto de la base de datos
        private readonly RecyRouteDbContext _context;

        // Constructor que recibe el contexto por inyección de dependencias
        public UserRepository(RecyRouteDbContext context)
        {
            _context = context;
        }

        // Obtiene todos los usuarios incluyendo su rol y tipo de documento
        public async Task<List<User>> GetUsers()
        {
            return await _context.Users
                                 .Include(u => u.Role)
                                 .Include(u => u.DocumentType)
                                 .ToListAsync();
        }

        // Obtiene un usuario específico por ID incluyendo su rol y tipo de documento
        public async Task<User> GetUser(Guid idUser)
        {
            return await _context.Users
                                 .Include(u => u.Role)
                                 .Include(u => u.DocumentType)
                                 .FirstOrDefaultAsync(u => u.IdUser == idUser);
        }

        // Obtiene todos los usuarios que pertenecen a un rol específico por nombre
        // Útil para notificar a todos los gestores cuando llega una nueva solicitud
        public async Task<List<User>> GetByRoleNameAsync(string roleName)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role!.RoleName == roleName)
                .ToListAsync();
        }

        // Crea un nuevo usuario y guarda los cambios en la base de datos
        // Recarga el usuario con Role y DocumentType para que el token y el DTO funcionen correctamente
        public async Task<User> CreateUser(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Recarga el usuario con sus relaciones después de guardarlo
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.DocumentType)
                .FirstOrDefaultAsync(u => u.IdUser == user.IdUser);
        }

        // Actualiza un usuario existente y guarda los cambios en la base de datos
        public async Task<User> UpdateUser(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        // Obtiene un usuario por su correo electrónico incluyendo rol y tipo de documento
        public async Task<User> GetUserByEmail(string email)
        {
            return await _context.Users
                                 .Include(u => u.Role)
                                 .Include(u => u.DocumentType)
                                 .FirstOrDefaultAsync(u => u.Email == email);
        }

        // Obtiene un usuario por su nombre incluyendo rol y tipo de documento
        public async Task<User> GetUserByName(string name)
        {
            return await _context.Users
                                 .Include(u => u.Role)
                                 .Include(u => u.DocumentType)
                                 .FirstOrDefaultAsync(u => u.Name == name);
        }

        // Elimina un usuario por su ID, retorna false si no existe
        public async Task<bool> DeleteUser(Guid idUser)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.IdUser == idUser);
            if (user == null)
                return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
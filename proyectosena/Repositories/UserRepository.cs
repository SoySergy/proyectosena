using Microsoft.EntityFrameworkCore;
using proyectosena.Context;
using proyectosena.Extensions;
using proyectosena.Interfaces.Repositories;
using proyectosena.Interfaces.Services;
using proyectosena.Models;

namespace proyectosena.Repositories
{
    public class UserRepository : IUserLookupRepository, IUserDirectoryRepository, IUserWriteRepository
    {
        // Contexto de la base de datos
        private readonly RecyRouteDbContext _context;

        // Constructor que recibe el contexto por inyección de dependencias
        public UserRepository(RecyRouteDbContext context)
        {
            _context = context;
        }

        // Obtiene todos los usuarios incluyendo su rol y tipo de documento
        // Página de usuarios con los filtros del panel. Cada uno se suma solo si
        // viene; sin ninguno, son los activos ordenados por nombre, como siempre.
        //
        // Solo para leer, y por eso sin seguimiento: nada de lo que devuelve
        // puede terminar guardado por accidente junto con otra entidad.
        public async Task<(List<User> Items, int Total)> GetUsers(UserQuery filter, int page, int pageSize)
        {
            var query = _context.Users
                                .AsNoTracking()
                                .Include(u => u.Role)
                                .Include(u => u.DocumentType)
                                .AsQueryable();

            if (filter.RoleName != null)
                query = query.Where(u => u.Role!.RoleName == filter.RoleName);

            if (filter.IsActive.HasValue)
                query = query.Where(u => u.IsActive == filter.IsActive.Value);

            if (filter.EmailVerified.HasValue)
                query = query.Where(u => u.IsEmailVerified == filter.EmailVerified.Value);

            if (filter.Search != null)
            {
                var pattern = "%" + filter.Search.EscapeForLike() + "%";
                query = query.Where(u =>
                    EF.Functions.ILike(u.Name + " " + u.LastName, pattern, SearchTextExtensions.LikeEscape) ||
                    EF.Functions.ILike(u.Email, pattern, SearchTextExtensions.LikeEscape) ||
                    EF.Functions.ILike(u.DocumentNumber, pattern, SearchTextExtensions.LikeEscape));
            }

            var ordered = (filter.OrderByRegistrationDate, filter.Ascending) switch
            {
                (true, true) => query.OrderBy(u => u.RegistrationDate),
                (true, false) => query.OrderByDescending(u => u.RegistrationDate),
                (false, true) => query.OrderBy(u => u.Name),
                (false, false) => query.OrderByDescending(u => u.Name)
            };

            // El id desempata: dos personas del mismo nombre podrían salir en dos
            // páginas seguidas y otra en ninguna.
            return await ordered.ThenBy(u => u.IdUser).ToPagedAsync(page, pageSize);
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
                .Where(u => u.Role!.RoleName == roleName && u.IsActive)
                .ToListAsync();
        }

        // Counts active users of a role. Asks the database for the number,
        // instead of loading every user just to count them.
        public async Task<int> CountByRole(string roleName)
        {
            return await _context.Users
                .CountAsync(u => u.Role!.RoleName == roleName && u.IsActive);
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
        //
        // La comparación va en minúsculas por los dos lados a propósito. SQL Server
        // no distinguía mayúsculas y esto daba igual, pero PostgreSQL sí: quien se
        // registró como "ana@x.com" no podía entrar escribiendo "Ana@X.com", y
        // tampoco recuperar su contraseña. Se comprobó al migrar: el mismo correo
        // devolvía 403 en minúsculas y 401 en mayúsculas.
        public async Task<User> GetUserByEmail(string email)
        {
            var normalizado = (email ?? string.Empty).Trim().ToLower();

            return await _context.Users
                                 .Include(u => u.Role)
                                 .Include(u => u.DocumentType)
                                 .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizado);
        }

        // Obtiene un usuario por su nombre incluyendo rol y tipo de documento

        // Busca un usuario por la combinación de número de documento Y tipo de documento
        // Un mismo número puede existir en diferentes tipos (cédula, pasaporte, etc.)
        // Solo se considera duplicado si coinciden ambos campos a la vez
        public async Task<User?> GetUserByDocument(string documentNumber, Guid idDocumentType)
        {
            return await _context.Users
                                 .Include(u => u.Role)
                                 .Include(u => u.DocumentType)
                                 .FirstOrDefaultAsync(u => u.DocumentNumber == documentNumber
                                                        && u.IdDocumentType == idDocumentType);
        }

        // Candado de asesoría propio para esta operación, distinto del que usa
        // MigrationExtensions (202609140001) para no competir con él por nada.
        private const long CandadoBajaDeUsuario = 202609140002;

        // Inactiva un usuario por su ID de forma atómica: bajo un candado de
        // Postgres, comprueba que no sea el último administrador activo y
        // aplica la baja lógica en el mismo tramo bloqueado (WA-16). El
        // candado cubre cualquier baja, no solo la de administradores, pero
        // dar de baja usuarios no es una ruta de alto tráfico.
        public async Task<UserDeactivationResult> DeactivateWithLastAdminGuard(Guid idUser, string administratorRoleName)
        {
            await _context.Database.OpenConnectionAsync();
            try
            {
                await _context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_lock({0})", CandadoBajaDeUsuario);
                try
                {
                    var user = await _context.Users
                                             .Include(u => u.Role)
                                             .FirstOrDefaultAsync(u => u.IdUser == idUser);
                    if (user == null)
                        return UserDeactivationResult.UserNotFound;

                    if (user.Role?.RoleName == administratorRoleName)
                    {
                        var activeAdmins = await _context.Users
                            .CountAsync(u => u.Role!.RoleName == administratorRoleName && u.IsActive);
                        if (activeAdmins <= 1)
                            return UserDeactivationResult.LastAdministrator;
                    }

                    user.IsActive = false;
                    await _context.SaveChangesAsync();
                    return UserDeactivationResult.Success;
                }
                finally
                {
                    await _context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock({0})", CandadoBajaDeUsuario);
                }
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }
    }
}
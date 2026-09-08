using Microsoft.EntityFrameworkCore;
using proyectosena.Context;
using proyectosena.Interfaces.Repositories;
using proyectosena.Interfaces.Services;
using proyectosena.Repositories;
using proyectosena.Services;

namespace proyectosena
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddProjectDependencies(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ── DbContext ──────────────────────────────
            string connectionString = configuration["ConnectionStrings:DefaultConnection"]
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            services.AddDbContext<RecyRouteDbContext>(options =>
                options.UseSqlServer(connectionString));

            // ── Repositories 
            // UserRepository cumple los tres contratos de usuario. Se registra la
            // clase concreta una sola vez y los tres interfaces la reenvían: así
            // una petición comparte una instancia en vez de crear tres.
            services.AddScoped<UserRepository>();
            services.AddScoped<IUserLookupRepository>(sp => sp.GetRequiredService<UserRepository>());
            services.AddScoped<IUserDirectoryRepository>(sp => sp.GetRequiredService<UserRepository>());
            services.AddScoped<IUserWriteRepository>(sp => sp.GetRequiredService<UserRepository>());
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IDocumentTypeRepository, DocumentTypeRepository>();
            services.AddScoped<ICollectionRequestRepository, CollectionRequestRepository>();
            services.AddScoped<ICollectionManagementRepository, CollectionManagementRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IHistoryRepository, HistoryRepository>();
            services.AddScoped<IChatHistoryRepository, ChatHistoryRepository>();
            services.AddScoped<IManagerApplicationRepository, ManagerApplicationRepository>();

            // ── Services ───────────────────────────────
            services.AddScoped<ICollectionStatusService, CollectionStatusService>();
            services.AddScoped<IAssignmentService, AssignmentService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IChatHistoryService, ChatHistoryService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICollectionRequestService, CollectionRequestService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IDocumentTypeService, DocumentTypeService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IHistoryService, HistoryService>();
            services.AddScoped<ICollectionManagementService, CollectionManagementService>();
            services.AddScoped<IManagerApplicationService, ManagerApplicationService>();

            // Singleton on purpose: PasswordResetService keeps the OTP codes in an
            // in-memory dictionary. As Scoped, every request would get an empty one
            // and no code would ever validate. EmailService is stateless, so a single
            // instance is enough.
            services.AddSingleton<IEmailService, EmailService>();
            services.AddSingleton<IPasswordResetService, PasswordResetService>();

            return services;
        }
    }
}
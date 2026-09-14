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
                options.UseNpgsql(connectionString));

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

            // EmailService no guarda estado, así que con una sola instancia basta.
            services.AddSingleton<IEmailService, EmailService>();

            // Scoped, no Singleton: los códigos y los tokens anulados viven en la base
            // (B-6 · WA-12) y estos servicios usan el DbContext, que es Scoped. Un
            // Singleton se quedaría con el DbContext de la primera petición.
            services.AddScoped<IVerificationCodeService, VerificationCodeService>();
            services.AddScoped<IRevokedTokenService, RevokedTokenService>();

            return services;
        }
    }
}
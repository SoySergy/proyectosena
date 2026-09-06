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
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IDocumentTypeRepository, DocumentTypeRepository>();
            services.AddScoped<ICollectionRequestRepository, CollectionRequestRepository>();
            services.AddScoped<ICollectionManagementRepository, CollectionManagementRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IHistoryRepository, HistoryRepository>();
            services.AddScoped<IChatHistoryRepository, ChatHistoryRepository>();

            // ── Services ───────────────────────────────
            services.AddScoped<ICollectionStatusService, CollectionStatusService>();
            services.AddScoped<IAssignmentService, AssignmentService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IChatHistoryService, ChatHistoryService>();

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
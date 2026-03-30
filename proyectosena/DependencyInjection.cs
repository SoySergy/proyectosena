using Microsoft.EntityFrameworkCore;
using proyectosena.Context;
using proyectosena.Interfaces;
using proyectosena.Repositories.Interfaces;
using proyectosena.Repositorios;
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

            services.AddScoped<ICollectionStatusService, CollectionStatusService>();
            services.AddScoped<IAssignmentService, AssignmentService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IPasswordResetService, PasswordResetService>();

            return services;
        }
    }
}
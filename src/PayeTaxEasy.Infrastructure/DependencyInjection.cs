using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PayeTaxEasy.Infrastructure.Data;
using PayeTaxEasy.Infrastructure.Services;

namespace PayeTaxEasy.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Database ──────────────────────────────────────────────────────────
        services.AddDbContext<PayeTaxEasyDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(PayeTaxEasyDbContext).Assembly.FullName)));

        // ── HTTP Client ───────────────────────────────────────────────────────
        services.AddHttpClient();

        // ── Services ──────────────────────────────────────────────────────────
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPayrollService, PayrollService>();
        services.AddScoped<IIrdIntegrationService, IrdIntegrationService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IEmployeePortalService, EmployeePortalService>();
        services.AddScoped<IIrdDashboardService, IrdDashboardService>();
        services.AddScoped<IIrdTinSearchService, IrdTinSearchService>();

        return services;
    }
}

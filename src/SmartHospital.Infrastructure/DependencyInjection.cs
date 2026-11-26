using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartHospital.Application.Admin;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Infrastructure.Files;
using SmartHospital.Infrastructure.Identity;
using SmartHospital.Infrastructure.Integrations;
using SmartHospital.Infrastructure.Persistence;
using SmartHospital.Infrastructure.Services;

namespace SmartHospital.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSmartHospitalInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<HospitalDbContext>((sp, options) =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var connectionString = cfg.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection chưa được cấu hình.");

            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(HospitalDbContext).Assembly.FullName);
            });
        });

        services.AddScoped<IHospitalDbContext>(sp => sp.GetRequiredService<HospitalDbContext>());

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
            options.User.RequireUniqueEmail = false;
        })
        .AddEntityFrameworkStores<HospitalDbContext>()
        .AddDefaultTokenProviders();

        services.AddMemoryCache();
        services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, CustomUserClaimsPrincipalFactory>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IRoleAdminService, RoleAdminService>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();
        services.AddScoped<IDataSeeder, HospitalDataSeeder>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IScopeAuthorizationService, ScopeAuthorizationService>();
        services.AddScoped<ICodeGeneratorService, CodeGeneratorService>();
        services.AddScoped<IDocumentStorage, LocalDocumentStorage>();

        // Demo/Sandbox external integration adapters
        services.AddScoped<ILabDeviceIntegration, DemoLabDeviceIntegration>();
        services.AddScoped<IRadiologyIntegration, DemoRadiologyIntegration>();
        services.AddScoped<IPacsIntegration, DemoPacsIntegration>();
        services.AddScoped<IInsuranceIntegration, DemoInsuranceIntegration>();
        services.AddScoped<IEInvoiceIntegration, DemoEInvoiceIntegration>();
        services.AddScoped<IPaymentGateway, DemoPaymentGateway>();
        services.AddScoped<INotificationProvider, DemoNotificationProvider>();

        return services;
    }
}

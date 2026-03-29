using Callio.Admin.Application.Tenants;
using Callio.Admin.Infrastructure.Persistence;
using Callio.Admin.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Callio.Admin.Infrastructure;

public static class AdminModuleExtensions
{
    public static IServiceCollection AddAdminModule(this IServiceCollection services, IConfiguration configuration)
    {
        
        services.AddScoped<ITenantRequestService, TenantRequestService>();
        services.AddDbContext<AdminDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("CallioDb")));
        
        return services;
    }
}
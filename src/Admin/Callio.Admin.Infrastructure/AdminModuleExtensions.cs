using Callio.Admin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Callio.Admin.Infrastructure;

public static class AdminModuleExtensions
{
    public static IServiceCollection AddAdminModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AdminDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("CallioDb")));
        
        return services;
    }
}
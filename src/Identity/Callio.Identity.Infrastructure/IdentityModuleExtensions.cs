using Callio.Identity.Domain;
using Callio.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Callio.Identity.Infrastructure;

public static class IdentityModuleExtensions
{
    public static void AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppIdentityDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("CallioDb")));

        services.AddIdentityApiEndpoints<ApplicationUser>()
            .AddEntityFrameworkStores<AppIdentityDbContext>();

        services.AddAuthentication();
        services.AddAuthorization();
    }

    public static void MapIdentityModule(this IEndpointRouteBuilder app)
    {
        app.MapIdentityApi<ApplicationUser>();
    }
}

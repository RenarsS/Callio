using Callio.Core.Domain.Constants.Identity;
using Callio.Core.Domain.Identity;
using Callio.Identity.Domain;
using Callio.Identity.Infrastructure.Identity;
using Callio.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Identity;
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
                configuration.GetConnectionString("CallioDb"),
                sqlOptions => sqlOptions
                    .MigrationsHistoryTable("__EFMigrationsHistory", "dbo")
                    .EnableRetryOnFailure()));

        services.AddIdentityApiEndpoints<ApplicationUser>()
            .AddEntityFrameworkStores<AppIdentityDbContext>();

        services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();
        services.AddScoped<IPortalUserContextAccessor, PortalUserContextAccessor>();

        services.AddAuthentication();
        services.AddAuthorizationBuilder()
            .AddPolicy(AppPolicies.PortalUser, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(AppClaims.UserType, UserType.TenantUser.ToString());
            });
    }

    public static void MapIdentityModule(this IEndpointRouteBuilder app)
    {
        app.MapIdentityApi<ApplicationUser>();
    }
}

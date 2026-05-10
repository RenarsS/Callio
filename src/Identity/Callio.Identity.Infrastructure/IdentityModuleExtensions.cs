using Callio.Core.Domain.Constants.Identity;
using Callio.Core.Domain.Identity;
using Callio.Identity.Domain;
using Callio.Identity.Infrastructure.Identity;
using Callio.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
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
                BuildIdentityConnectionString(configuration),
                sqlOptions => sqlOptions
                    .MigrationsHistoryTable("__EFMigrationsHistory", "dbo")
                    .CommandTimeout(60)
                    .EnableRetryOnFailure(
                        maxRetryCount: 10,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: [40613])));

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

    private static string BuildIdentityConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CallioDb")
            ?? throw new InvalidOperationException("Connection string 'CallioDb' is required for identity storage.");

        var builder = new SqlConnectionStringBuilder(connectionString);
        builder.ConnectTimeout = Math.Max(60, builder.ConnectTimeout);
        builder.ConnectRetryCount = 3;
        builder.ConnectRetryInterval = 10;
        builder.MultipleActiveResultSets = true;

        return builder.ConnectionString;
    }
}

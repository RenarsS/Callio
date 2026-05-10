using Callio.Admin.Application.Tenants;
using Callio.Admin.Infrastructure.Persistence;
using Callio.Admin.Infrastructure.Services;
using Microsoft.Data.SqlClient;
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
                BuildAdminConnectionString(configuration),
                sqlOptions => sqlOptions
                    .MigrationsHistoryTable("__EFMigrationsHistory", "dbo")
                    .CommandTimeout(60)
                    .EnableRetryOnFailure(
                        maxRetryCount: 10,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: [40613])));
        
        return services;
    }

    private static string BuildAdminConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CallioDb")
            ?? throw new InvalidOperationException("Connection string 'CallioDb' is required for admin storage.");

        var builder = new SqlConnectionStringBuilder(connectionString);
        builder.ConnectTimeout = Math.Max(60, builder.ConnectTimeout);
        builder.ConnectRetryCount = 3;
        builder.ConnectRetryInterval = 10;
        builder.MultipleActiveResultSets = true;

        return builder.ConnectionString;
    }
}

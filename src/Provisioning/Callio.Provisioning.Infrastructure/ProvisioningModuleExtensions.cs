using Callio.Provisioning.Application;
using Callio.Provisioning.Infrastructure.Options;
using Callio.Provisioning.Infrastructure.Persistence;
using Callio.Provisioning.Infrastructure.Provisioners;
using Callio.Provisioning.Infrastructure.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Callio.Provisioning.Infrastructure;

public static class ProvisioningModuleExtensions
{
    public static IServiceCollection AddProvisioningModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TenantProvisioningOptions>(configuration.GetSection(TenantProvisioningOptions.SectionName));

        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
        services.AddSingleton<ITenantDatabaseConnectionStringFactory, TenantDatabaseConnectionStringFactory>();
        services.AddScoped<ITenantDatabaseSchemaProvisioner, SqlServerTenantDatabaseSchemaProvisioner>();
        services.AddSingleton<TenantVectorStoreCosmosContext>();
        services.AddScoped<DevelopmentTenantVectorStoreProvisioner>();
        services.AddScoped<AzureCosmosTenantVectorStoreProvisioner>();
        services.AddScoped<ITenantVectorStoreProvisioner, TenantVectorStoreProvisioner>();
        services.AddScoped<LocalTenantBlobStorageProvisioner>();
        services.AddScoped<AzureBlobTenantBlobStorageProvisioner>();
        services.AddScoped<ITenantBlobStorageProvisioner, TenantBlobStorageProvisioner>();
        services.AddSingleton<ITenantResourceNamingStrategy, DefaultTenantResourceNamingStrategy>();

        services.AddDbContext<ProvisioningDbContext>(options =>
            options.UseSqlServer(
                BuildProvisioningConnectionString(configuration),
                sqlOptions => sqlOptions
                    .MigrationsHistoryTable(
                        SqlServerTransientRetry.MigrationsHistoryTable,
                        SqlServerTransientRetry.MigrationsHistorySchema)
                    .CommandTimeout(60)
                    .EnableRetryOnFailure(
                        SqlServerTransientRetry.MaxRetryCount,
                        SqlServerTransientRetry.MaxRetryDelay,
                        SqlServerTransientRetry.AdditionalErrorNumbers)));

        return services;
    }

    private static string BuildProvisioningConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CallioDb")
            ?? throw new InvalidOperationException("Connection string 'CallioDb' is required for provisioning storage.");

        var builder = new SqlConnectionStringBuilder(connectionString);
        builder.ConnectTimeout = Math.Max(60, builder.ConnectTimeout);
        builder.ConnectRetryCount = 3;
        builder.ConnectRetryInterval = 10;
        builder.MultipleActiveResultSets = true;

        return builder.ConnectionString;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Callio.Provisioning.Infrastructure.Services;

namespace Callio.Generation.Infrastructure.Persistence;

public class TenantGenerationDbContextFactory(
    ITenantDatabaseConnectionStringFactory connectionStringFactory) : ITenantGenerationDbContextFactory
{
    public TenantGenerationDbContext Create(string schemaName)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenantGenerationDbContext>();
        optionsBuilder
            .UseSqlServer(
                connectionStringFactory.CreateTenantConnectionString(),
                sqlOptions => sqlOptions
                    .MigrationsHistoryTable(
                        SqlServerTransientRetry.MigrationsHistoryTable,
                        SqlServerTransientRetry.MigrationsHistorySchema)
                    .EnableRetryOnFailure(
                        SqlServerTransientRetry.MaxRetryCount,
                        SqlServerTransientRetry.MaxRetryDelay,
                        SqlServerTransientRetry.AdditionalErrorNumbers))
            .ReplaceService<IModelCacheKeyFactory, TenantGenerationModelCacheKeyFactory>();

        return new TenantGenerationDbContext(optionsBuilder.Options, schemaName);
    }
}

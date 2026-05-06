using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Callio.Generation.Infrastructure.Persistence;

public class TenantGenerationDbContextFactory(
    ITenantGenerationDatabaseConnectionStringFactory connectionStringFactory) : ITenantGenerationDbContextFactory
{
    public TenantGenerationDbContext Create(string schemaName)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenantGenerationDbContext>();
        optionsBuilder
            .UseSqlServer(
                connectionStringFactory.CreateTenantConnectionString(),
                sqlOptions => sqlOptions
                    .MigrationsHistoryTable(
                        SqlServerGenerationTransientRetry.MigrationsHistoryTable,
                        SqlServerGenerationTransientRetry.MigrationsHistorySchema)
                    .EnableRetryOnFailure(
                        SqlServerGenerationTransientRetry.MaxRetryCount,
                        SqlServerGenerationTransientRetry.MaxRetryDelay,
                        SqlServerGenerationTransientRetry.AdditionalErrorNumbers))
            .ReplaceService<IModelCacheKeyFactory, TenantGenerationModelCacheKeyFactory>();

        return new TenantGenerationDbContext(optionsBuilder.Options, schemaName);
    }
}

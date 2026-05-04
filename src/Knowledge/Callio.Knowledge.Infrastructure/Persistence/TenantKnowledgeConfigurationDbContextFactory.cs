using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Callio.Provisioning.Infrastructure.Services;

namespace Callio.Knowledge.Infrastructure.Persistence;

public class TenantKnowledgeConfigurationDbContextFactory(
    ITenantDatabaseConnectionStringFactory connectionStringFactory) : ITenantKnowledgeConfigurationDbContextFactory
{
    public TenantKnowledgeConfigurationDbContext Create(string schemaName)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenantKnowledgeConfigurationDbContext>();
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
            .ReplaceService<IModelCacheKeyFactory, TenantKnowledgeConfigurationModelCacheKeyFactory>();

        return new TenantKnowledgeConfigurationDbContext(optionsBuilder.Options, schemaName);
    }
}

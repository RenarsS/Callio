using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Callio.Provisioning.Infrastructure.Services;

namespace Callio.Knowledge.Infrastructure.Persistence;

public class TenantKnowledgeDocumentDbContextFactory(
    ITenantDatabaseConnectionStringFactory connectionStringFactory) : ITenantKnowledgeDocumentDbContextFactory
{
    public TenantKnowledgeDocumentDbContext Create(string schemaName)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenantKnowledgeDocumentDbContext>();
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
            .ReplaceService<IModelCacheKeyFactory, TenantKnowledgeDocumentModelCacheKeyFactory>();

        return new TenantKnowledgeDocumentDbContext(optionsBuilder.Options, schemaName);
    }
}

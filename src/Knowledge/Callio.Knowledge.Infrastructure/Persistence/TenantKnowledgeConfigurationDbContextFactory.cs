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
            .UseSqlServer(connectionStringFactory.CreateTenantConnectionString())
            .ReplaceService<IModelCacheKeyFactory, TenantKnowledgeConfigurationModelCacheKeyFactory>();

        return new TenantKnowledgeConfigurationDbContext(optionsBuilder.Options, schemaName);
    }
}

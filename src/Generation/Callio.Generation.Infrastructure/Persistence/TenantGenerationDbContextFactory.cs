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
            .UseSqlServer(connectionStringFactory.CreateTenantConnectionString())
            .ReplaceService<IModelCacheKeyFactory, TenantGenerationModelCacheKeyFactory>();

        return new TenantGenerationDbContext(optionsBuilder.Options, schemaName);
    }
}

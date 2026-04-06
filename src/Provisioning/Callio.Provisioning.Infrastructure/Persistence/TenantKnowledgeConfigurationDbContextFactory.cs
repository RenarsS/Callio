using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace Callio.Provisioning.Infrastructure.Persistence;

public class TenantKnowledgeConfigurationDbContextFactory(IConfiguration configuration) : ITenantKnowledgeConfigurationDbContextFactory
{
    private readonly string _connectionString = configuration.GetConnectionString("CallioTenantsDb")
        ?? throw new InvalidOperationException("A CallioTenantsDb connection string is required for tenant knowledge configuration storage.");

    public TenantKnowledgeConfigurationDbContext Create(string schemaName)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenantKnowledgeConfigurationDbContext>();
        optionsBuilder.UseSqlServer(_connectionString);
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, TenantKnowledgeConfigurationModelCacheKeyFactory>();

        return new TenantKnowledgeConfigurationDbContext(optionsBuilder.Options, schemaName);
    }
}

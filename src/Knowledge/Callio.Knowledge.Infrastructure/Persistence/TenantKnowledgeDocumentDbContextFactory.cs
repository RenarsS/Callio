using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace Callio.Knowledge.Infrastructure.Persistence;

public class TenantKnowledgeDocumentDbContextFactory(IConfiguration configuration) : ITenantKnowledgeDocumentDbContextFactory
{
    private readonly string _connectionString = configuration.GetConnectionString("CallioTenantsDb")
        ?? throw new InvalidOperationException("A CallioTenantsDb connection string is required for tenant knowledge document storage.");

    public TenantKnowledgeDocumentDbContext Create(string schemaName)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenantKnowledgeDocumentDbContext>();
        optionsBuilder.UseSqlServer(_connectionString);
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, TenantKnowledgeDocumentModelCacheKeyFactory>();

        return new TenantKnowledgeDocumentDbContext(optionsBuilder.Options, schemaName);
    }
}

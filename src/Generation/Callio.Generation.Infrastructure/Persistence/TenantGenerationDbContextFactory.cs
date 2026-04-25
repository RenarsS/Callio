using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace Callio.Generation.Infrastructure.Persistence;

public class TenantGenerationDbContextFactory(IConfiguration configuration) : ITenantGenerationDbContextFactory
{
    private readonly string _connectionString = configuration.GetConnectionString("CallioTenantsDb")
        ?? throw new InvalidOperationException("A CallioTenantsDb connection string is required for tenant generation storage.");

    public TenantGenerationDbContext Create(string schemaName)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TenantGenerationDbContext>();
        optionsBuilder.UseSqlServer(_connectionString);
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, TenantGenerationModelCacheKeyFactory>();

        return new TenantGenerationDbContext(optionsBuilder.Options, schemaName);
    }
}

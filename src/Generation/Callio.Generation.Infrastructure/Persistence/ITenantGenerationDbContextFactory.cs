namespace Callio.Generation.Infrastructure.Persistence;

public interface ITenantGenerationDbContextFactory
{
    TenantGenerationDbContext Create(string schemaName);
}

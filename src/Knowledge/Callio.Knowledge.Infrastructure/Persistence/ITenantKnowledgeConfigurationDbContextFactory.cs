namespace Callio.Knowledge.Infrastructure.Persistence;

public interface ITenantKnowledgeConfigurationDbContextFactory
{
    TenantKnowledgeConfigurationDbContext Create(string schemaName);
}

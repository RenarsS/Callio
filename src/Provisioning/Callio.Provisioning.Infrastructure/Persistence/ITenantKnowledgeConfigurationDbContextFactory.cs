namespace Callio.Provisioning.Infrastructure.Persistence;

public interface ITenantKnowledgeConfigurationDbContextFactory
{
    TenantKnowledgeConfigurationDbContext Create(string schemaName);
}

namespace Callio.Provisioning.Infrastructure.Persistence;

public interface ITenantKnowledgeDocumentDbContextFactory
{
    TenantKnowledgeDocumentDbContext Create(string schemaName);
}

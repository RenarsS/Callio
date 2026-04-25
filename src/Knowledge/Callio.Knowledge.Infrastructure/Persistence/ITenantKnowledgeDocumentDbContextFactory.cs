namespace Callio.Knowledge.Infrastructure.Persistence;

public interface ITenantKnowledgeDocumentDbContextFactory
{
    TenantKnowledgeDocumentDbContext Create(string schemaName);
}

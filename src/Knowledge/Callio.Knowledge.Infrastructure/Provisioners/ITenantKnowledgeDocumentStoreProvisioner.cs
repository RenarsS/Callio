namespace Callio.Knowledge.Infrastructure.Provisioners;

public interface ITenantKnowledgeDocumentStoreProvisioner
{
    Task EnsureCreatedAsync(string schemaName, CancellationToken cancellationToken = default);
}

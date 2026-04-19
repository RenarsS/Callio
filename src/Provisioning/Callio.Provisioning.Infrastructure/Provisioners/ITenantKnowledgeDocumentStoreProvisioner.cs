namespace Callio.Provisioning.Infrastructure.Provisioners;

public interface ITenantKnowledgeDocumentStoreProvisioner
{
    Task EnsureCreatedAsync(string schemaName, CancellationToken cancellationToken = default);
}

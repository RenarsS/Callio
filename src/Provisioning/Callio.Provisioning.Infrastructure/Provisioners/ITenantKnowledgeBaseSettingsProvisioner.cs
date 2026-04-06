namespace Callio.Provisioning.Infrastructure.Provisioners;

public interface ITenantKnowledgeBaseSettingsProvisioner
{
    Task EnsureCreatedAsync(
        int tenantId,
        string databaseSchema,
        string vectorStoreNamespace,
        CancellationToken cancellationToken = default);
}

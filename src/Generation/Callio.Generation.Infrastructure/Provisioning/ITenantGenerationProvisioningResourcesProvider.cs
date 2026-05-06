namespace Callio.Generation.Infrastructure.Provisioning;

public interface ITenantGenerationProvisioningResourcesProvider
{
    Task<TenantGenerationProvisioningResources> GetAsync(
        int tenantId,
        CancellationToken cancellationToken = default);
}

public record TenantGenerationProvisioningResources(
    int TenantId,
    string DatabaseSchema,
    string VectorStoreNamespace,
    string BlobContainerName);

namespace Callio.Core.Infrastructure.Messaging.Tenants;

public record GetTenantProvisioningResourcesRequest(int TenantId);

public record GetTenantProvisioningResourcesResponse(
    int TenantId,
    string DatabaseSchema,
    string VectorStoreNamespace,
    string BlobContainerName);

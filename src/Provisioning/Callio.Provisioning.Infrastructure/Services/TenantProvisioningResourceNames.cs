namespace Callio.Provisioning.Infrastructure.Services;

public sealed record TenantProvisioningResourceNames(
    string DatabaseSchema,
    string VectorStoreNamespace,
    string BlobContainerName);

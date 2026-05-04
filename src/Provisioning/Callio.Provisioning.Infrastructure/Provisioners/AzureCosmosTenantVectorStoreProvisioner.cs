using Callio.Provisioning.Infrastructure.Services;

namespace Callio.Provisioning.Infrastructure.Provisioners;

public class AzureCosmosTenantVectorStoreProvisioner(
    TenantVectorStoreCosmosContext cosmosContext) : ITenantVectorStoreProvisioner
{
    public async Task EnsureCreatedAsync(int tenantId, string namespaceName, CancellationToken cancellationToken = default)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantId));

        if (string.IsNullOrWhiteSpace(namespaceName))
            throw new ArgumentException("Namespace name is required.", nameof(namespaceName));

        await cosmosContext.CreateVectorContainerIfNotExistsAsync(namespaceName, cancellationToken);
    }
}

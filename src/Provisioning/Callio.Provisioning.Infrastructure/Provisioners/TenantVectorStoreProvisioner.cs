using Callio.Provisioning.Infrastructure.Services;

namespace Callio.Provisioning.Infrastructure.Provisioners;

public class TenantVectorStoreProvisioner(
    TenantVectorStoreCosmosContext cosmosContext,
    DevelopmentTenantVectorStoreProvisioner developmentProvisioner,
    AzureCosmosTenantVectorStoreProvisioner azureCosmosProvisioner) : ITenantVectorStoreProvisioner
{
    public Task EnsureCreatedAsync(int tenantId, string namespaceName, CancellationToken cancellationToken = default)
        => cosmosContext.UsesAzureCosmos
            ? azureCosmosProvisioner.EnsureCreatedAsync(tenantId, namespaceName, cancellationToken)
            : developmentProvisioner.EnsureCreatedAsync(tenantId, namespaceName, cancellationToken);
}

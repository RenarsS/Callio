using Callio.Core.Infrastructure.Messaging.Tenants;
using MassTransit;

namespace Callio.Generation.Infrastructure.Provisioning;

public class BrokeredTenantGenerationProvisioningResourcesProvider(
    IRequestClient<GetTenantProvisioningResourcesRequest> requestClient)
    : ITenantGenerationProvisioningResourcesProvider
{
    public async Task<TenantGenerationProvisioningResources> GetAsync(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        var response = await requestClient.GetResponse<GetTenantProvisioningResourcesResponse>(
            new GetTenantProvisioningResourcesRequest(tenantId),
            cancellationToken);

        return new TenantGenerationProvisioningResources(
            response.Message.TenantId,
            response.Message.DatabaseSchema,
            response.Message.VectorStoreNamespace,
            response.Message.BlobContainerName);
    }
}

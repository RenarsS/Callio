using Callio.Core.Infrastructure.Messaging.Tenants;
using Callio.Provisioning.Infrastructure.Persistence;
using Callio.Provisioning.Infrastructure.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Callio.Provisioning.Infrastructure.Consumers;

public class GetTenantProvisioningResourcesConsumer(
    ProvisioningDbContext provisioningDbContext,
    ITenantResourceNamingStrategy tenantResourceNamingStrategy)
    : IConsumer<GetTenantProvisioningResourcesRequest>
{
    public async Task Consume(ConsumeContext<GetTenantProvisioningResourcesRequest> context)
    {
        var tenantId = context.Message.TenantId;
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(context.Message.TenantId), "Tenant id must be greater than zero.");

        var existing = await provisioningDbContext.TenantInfrastructureProvisionings
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .Select(x => new
            {
                x.DatabaseSchema,
                x.VectorStoreNamespace,
                x.BlobContainerName
            })
            .FirstOrDefaultAsync(context.CancellationToken);

        var resources = existing is not null
            ? new GetTenantProvisioningResourcesResponse(
                tenantId,
                existing.DatabaseSchema,
                existing.VectorStoreNamespace,
                existing.BlobContainerName)
            : MapFallback(tenantId, tenantResourceNamingStrategy.Create(tenantId));

        await context.RespondAsync(resources);
    }

    private static GetTenantProvisioningResourcesResponse MapFallback(
        int tenantId,
        TenantProvisioningResourceNames names)
        => new(
            tenantId,
            names.DatabaseSchema,
            names.VectorStoreNamespace,
            names.BlobContainerName);
}

using Callio.Core.Infrastructure.Messaging.Tenants;
using Callio.Provisioning.Application;
using MassTransit;

namespace Callio.Provisioning.Infrastructure.Consumers;

public class TenantApprovedProvisioningConsumer(ITenantProvisioningService tenantProvisioningService) : IConsumer<TenantApprovedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<TenantApprovedIntegrationEvent> context)
    {
        await tenantProvisioningService.HandleTenantApprovedAsync(
            new TenantApprovedProvisioningCommand(
                context.Message.UserId,
                context.Message.TenantId,
                context.Message.TenantRequestId),
            context.CancellationToken);
    }
}

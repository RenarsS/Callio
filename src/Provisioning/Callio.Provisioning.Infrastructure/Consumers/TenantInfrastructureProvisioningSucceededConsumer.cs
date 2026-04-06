using Callio.Core.Infrastructure.Messaging.Tenants;
using Callio.Provisioning.Application.KnowledgeConfigurations;
using MassTransit;

namespace Callio.Provisioning.Infrastructure.Consumers;

public class TenantInfrastructureProvisioningSucceededConsumer(
    ITenantKnowledgeConfigurationSetupService tenantKnowledgeConfigurationSetupService)
    : IConsumer<TenantInfrastructureProvisioningSucceededIntegrationEvent>
{
    public async Task Consume(ConsumeContext<TenantInfrastructureProvisioningSucceededIntegrationEvent> context)
    {
        await tenantKnowledgeConfigurationSetupService.HandleProvisioningSucceededAsync(
            new RunTenantKnowledgeConfigurationSetupCommand(context.Message.TenantId),
            context.CancellationToken);
    }
}

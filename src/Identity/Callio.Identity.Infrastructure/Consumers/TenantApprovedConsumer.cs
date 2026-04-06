using Callio.Core.Infrastructure.Messaging.Tenants;
using Callio.Identity.Domain;
using MassTransit;
using Microsoft.AspNetCore.Identity;

namespace Callio.Identity.Infrastructure.Consumers;

public class TenantApprovedConsumer(UserManager<ApplicationUser> userManager) : IConsumer<TenantApprovedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<TenantApprovedIntegrationEvent> context)
    {
        var user = await userManager.FindByIdAsync(context.Message.UserId);
        if (user is null || user.TenantId == context.Message.TenantId)
            return;

        user.LinkToTenant(context.Message.TenantId);
        await userManager.UpdateAsync(user);
    }
}

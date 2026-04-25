using Carter;
using Callio.Knowledge.Application.KnowledgeConfigurations;
using Callio.Provisioning.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Callio.Knowledge.API.Modules;

public class TenantKnowledgeConfigurationSetupModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/internal/tenant-infrastructure")
            .WithTags("Tenant Knowledge Configuration Setup");

        group.MapPost("/{tenantId:int}/knowledge-configuration/retry", async (
            int tenantId,
            ITenantKnowledgeConfigurationSetupService setupService,
            ITenantProvisioningService provisioningService,
            CancellationToken cancellationToken) =>
        {
            var setupResult = await setupService.RetryAsync(
                new RunTenantKnowledgeConfigurationSetupCommand(tenantId),
                cancellationToken);

            if (setupResult is null)
                return Results.BadRequest("Tenant knowledge configuration setup can only be retried after infrastructure provisioning succeeds.");

            var status = await provisioningService.GetStatusAsync(tenantId, cancellationToken);
            return status is null ? Results.NotFound() : Results.Ok(status);
        });
    }
}

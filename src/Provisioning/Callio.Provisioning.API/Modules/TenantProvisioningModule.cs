using Carter;
using Callio.Provisioning.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Callio.Provisioning.API.Modules;

public class TenantProvisioningModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/internal/tenant-infrastructure")
            .WithTags("Tenant Infrastructure");

        group.MapGet(string.Empty, async (ITenantProvisioningService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAllStatusesAsync(cancellationToken)));

        group.MapGet("/{tenantId:int}", async (int tenantId, ITenantProvisioningService service, CancellationToken cancellationToken) =>
        {
            var result = await service.GetStatusAsync(tenantId, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapPost("/{tenantId:int}/retry", async (int tenantId, ITenantProvisioningService service, CancellationToken cancellationToken) =>
        {
            var result = await service.RetryFailedAsync(tenantId, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapPost("/{tenantId:int}/reprovision", async (int tenantId, ITenantProvisioningService service, CancellationToken cancellationToken) =>
        {
            var result = await service.ReprovisionAsync(tenantId, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });
    }
}

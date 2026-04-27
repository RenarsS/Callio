using Carter;
using Callio.Core.Domain.Constants.Identity;
using Callio.Core.Domain.Identity;
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
        var portal = app.MapGroup("api/portal/tenants/{tenantId:int}/provisioning")
            .WithTags("Portal Tenant Provisioning")
            .RequireAuthorization(AppPolicies.PortalUser);

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

        portal.MapGet(string.Empty, async (
            int tenantId,
            ITenantProvisioningService service,
            HttpContext httpContext,
            IPortalUserContextAccessor portalUserContextAccessor,
            CancellationToken cancellationToken) =>
        {
            var accessError = await EnsurePortalTenantAccessAsync(tenantId, httpContext, portalUserContextAccessor, cancellationToken);
            if (accessError is not null)
                return accessError;

            var result = await service.GetStatusAsync(tenantId, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });
    }

    private static async Task<IResult?> EnsurePortalTenantAccessAsync(
        int tenantId,
        HttpContext httpContext,
        IPortalUserContextAccessor portalUserContextAccessor,
        CancellationToken cancellationToken)
    {
        if (tenantId <= 0)
            return Results.BadRequest("Tenant id must be greater than zero.");

        var currentUser = await portalUserContextAccessor.GetCurrentAsync(httpContext.User, cancellationToken);
        if (currentUser is null)
            return Results.Unauthorized();

        return currentUser.TenantId == tenantId ? null : Results.Forbid();
    }
}

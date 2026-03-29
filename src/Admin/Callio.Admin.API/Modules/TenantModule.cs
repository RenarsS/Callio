using Carter;
using Callio.Admin.API.Contracts.Tenants;
using Callio.Admin.Application.Tenants;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Callio.Admin.API.Modules;

public class TenantModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var portal = app.MapGroup("api/portal/tenant-requests").WithTags("Portal Tenant Requests");
        portal.MapPost("/", async (CreateTenantRequestRequest request, ITenantRequestService service, CancellationToken cancellationToken) =>
        {
            var result = await service.CreateAsync(new CreateTenantRequestCommand(
                request.TenantName,
                request.RequestedByUserId,
                request.RequestedByEmail,
                request.RequestedByFirstName,
                request.RequestedByLastName,
                request.CompanyName,
                request.Notes), cancellationToken);

            return Results.Created($"/api/portal/tenant-requests/{result.Id}", result);
        });

        var dashboard = app.MapGroup("api/dashboard/tenant-requests").WithTags("Dashboard Tenant Requests");
        dashboard.MapGet("/", async (ITenantRequestService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAllAsync(cancellationToken)));

        dashboard.MapPost("/{requestId:int}/approve", async (int requestId, ProcessTenantRequestRequest request, ITenantRequestService service, CancellationToken cancellationToken) =>
        {
            var result = await service.ApproveAsync(new ProcessTenantRequestCommand(requestId, request.ProcessedByUserId, request.DecisionNote), cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        dashboard.MapPost("/{requestId:int}/reject", async (int requestId, ProcessTenantRequestRequest request, ITenantRequestService service, CancellationToken cancellationToken) =>
        {
            var result = await service.RejectAsync(new ProcessTenantRequestCommand(requestId, request.ProcessedByUserId, request.DecisionNote), cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });
    }
}

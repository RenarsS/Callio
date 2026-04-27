using Carter;
using Callio.Admin.API.Contracts.Tenants;
using Callio.Admin.Application.Tenants;
using Callio.Core.Domain.Constants.Identity;
using Callio.Core.Domain.Identity;
using Callio.Admin.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Callio.Admin.API.Modules;

public class TenantModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var portal = app.MapGroup("api/portal").WithTags("Portal Tenant Onboarding");
        portal.MapPost("/tenant-onboarding", async (RegisterPortalUserAndTenantRequest request, ITenantRequestService service, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.RegisterPortalUserAndRequestTenantAsync(
                    new RegisterPortalUserAndTenantCommand(
                        request.Email,
                        request.Password,
                        request.FirstName,
                        request.LastName,
                        request.CompanyName,
                        request.TenantName,
                        request.SelectedPlanId,
                        request.Notes),
                    cancellationToken);

                return Results.Created($"/api/portal/tenant-requests/{result.TenantRequestId}", result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        portal.MapGet("/me", async (
                HttpContext httpContext,
                IPortalUserContextAccessor portalUserContextAccessor,
                CancellationToken cancellationToken) =>
            {
                var currentUser = await portalUserContextAccessor.GetCurrentAsync(httpContext.User, cancellationToken);
                return currentUser is null
                    ? Results.Unauthorized()
                    : Results.Ok(new PortalUserProfileResponse(
                        currentUser.UserId,
                        currentUser.Email,
                        currentUser.DisplayName,
                        currentUser.UserType,
                        currentUser.TenantId));
            })
            .RequireAuthorization(AppPolicies.PortalUser);

        portal.MapGet("/tenant-requests/current", async (
                ITenantRequestService service,
                HttpContext httpContext,
                IPortalUserContextAccessor portalUserContextAccessor,
                CancellationToken cancellationToken) =>
            {
                var currentUser = await portalUserContextAccessor.GetCurrentAsync(httpContext.User, cancellationToken);
                if (currentUser is null)
                    return Results.Unauthorized();

                var result = await service.GetLatestPortalStatusForUserAsync(currentUser.UserId, cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            })
            .RequireAuthorization(AppPolicies.PortalUser);

        portal.MapGet("/tenant-requests/{requestId:int}", async (int requestId, ITenantRequestService service, HttpContext httpContext, IPortalUserContextAccessor portalUserContextAccessor, CancellationToken cancellationToken) =>
        {
            var currentUser = await portalUserContextAccessor.GetCurrentAsync(httpContext.User, cancellationToken);
            if (currentUser is null)
                return Results.Unauthorized();

            var result = await service.GetPortalStatusAsync(requestId, currentUser.Email, cancellationToken);
            if (result is null)
                return Results.NotFound();

            return string.Equals(result.RequestedByEmail, currentUser.Email, StringComparison.OrdinalIgnoreCase)
                ? Results.Ok(result)
                : Results.Forbid();
        }).RequireAuthorization(AppPolicies.PortalUser);

        portal.MapGet("/tenant-requests/by-tenant/{tenantId:int}", async (int tenantId, ITenantRequestService service, HttpContext httpContext, IPortalUserContextAccessor portalUserContextAccessor, CancellationToken cancellationToken) =>
        {
            var currentUser = await portalUserContextAccessor.GetCurrentAsync(httpContext.User, cancellationToken);
            if (currentUser is null)
                return Results.Unauthorized();

            if (currentUser.TenantId != tenantId)
                return Results.Forbid();

            var result = await service.GetPortalStatusByTenantIdAsync(tenantId, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequireAuthorization(AppPolicies.PortalUser);

        var dashboard = app.MapGroup("api/dashboard").WithTags("Dashboard Tenants");
        dashboard.MapGet("/tenant-requests", async (ITenantRequestService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAllAsync(cancellationToken)));

        dashboard.MapPost("/tenant-requests/{requestId:int}/approve", async (int requestId, ProcessTenantRequestRequest request, ITenantRequestService service, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.ApproveAsync(new ProcessTenantRequestCommand(requestId, request.ProcessedByUserId, request.DecisionNote), cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        dashboard.MapPost("/tenant-requests/{requestId:int}/reject", async (int requestId, ProcessTenantRequestRequest request, ITenantRequestService service, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.RejectAsync(new ProcessTenantRequestCommand(requestId, request.ProcessedByUserId, request.DecisionNote), cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        dashboard.MapGet("/tenants", async (AdminDbContext db, CancellationToken ct) =>
        {
            var tenants = await db.Tenants
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new TenantListItemResponse(
                    x.Id,
                    x.Name,
                    x.TenantCode,
                    x.Contact.Person,
                    x.Contact.Email,
                    x.CreatedAt,
                    x.ActivatedAt,
                    x.DeactivatedAt,
                    x.Status.Value.ToString(),
                    db.Subscriptions.Where(s => s.TenantId == x.Id)
                        .OrderByDescending(s => s.Id)
                        .Join(db.Plans, s => s.PlanId, p => p.Id, (s, p) => p.Name)
                        .FirstOrDefault(),
                    db.Subscriptions.Where(s => s.TenantId == x.Id)
                        .OrderByDescending(s => s.Id)
                        .Select(s => s.Status.ToString())
                        .FirstOrDefault()))
                .ToListAsync(ct);

            return Results.Ok(tenants);
        });
    }
}

public record PortalUserProfileResponse(
    string UserId,
    string Email,
    string DisplayName,
    string UserType,
    int? TenantId);

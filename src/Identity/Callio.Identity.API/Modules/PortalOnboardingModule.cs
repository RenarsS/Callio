using Carter;
using Callio.Identity.API.Contracts.PortalOnboarding;
using Callio.Identity.Application.PortalOnboarding;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Callio.Identity.API.Modules;

public class PortalOnboardingModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var portal = app.MapGroup("api/portal/onboarding").WithTags("Portal Onboarding");

        portal.MapPost("/register-tenant", async (
            [FromBody] RegisterPortalUserAndTenantRequest request,
            [FromServices] IPortalOnboardingService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.RegisterPortalUserAndRequestTenantAsync(
                new RegisterPortalUserAndTenantCommand(
                    request.Email,
                    request.Password,
                    request.FirstName,
                    request.LastName,
                    request.CompanyName,
                    request.TenantName,
                    request.Notes),
                cancellationToken);

            return Results.Created($"/api/dashboard/tenant-requests/{result.TenantRequestId}", result);
        });
    }
}
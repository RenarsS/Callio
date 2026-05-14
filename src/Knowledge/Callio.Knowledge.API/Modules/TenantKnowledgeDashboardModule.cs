using Carter;
using Callio.Core.Domain.Constants.Identity;
using Callio.Knowledge.Application.KnowledgeDocuments;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Callio.Knowledge.API.Modules;

public class TenantKnowledgeDashboardModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var dashboard = app.MapGroup("api/dashboard/knowledge")
            .WithTags("Tenant Knowledge Dashboard")
            .RequireAuthorization(AppPolicies.DashboardAdmin);

        dashboard.MapGet("/overview", async (ITenantKnowledgeDashboardService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetOverviewAsync(cancellationToken)));
    }
}

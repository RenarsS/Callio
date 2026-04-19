using Carter;
using Callio.Provisioning.Application.KnowledgeDocuments;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Callio.Provisioning.API.Modules;

public class TenantKnowledgeDashboardModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var dashboard = app.MapGroup("api/dashboard/knowledge")
            .WithTags("Tenant Knowledge Dashboard");

        dashboard.MapGet("/overview", async (ITenantKnowledgeDashboardService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetOverviewAsync(cancellationToken)));
    }
}

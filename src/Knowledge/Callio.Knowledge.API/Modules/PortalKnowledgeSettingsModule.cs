using Carter;
using Callio.Core.Domain.Exceptions;
using Callio.Knowledge.Application.KnowledgeConfigurations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Callio.Knowledge.API.Modules;

public class PortalKnowledgeSettingsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var portal = app.MapGroup("api/portal/tenants/{tenantId:int}/knowledge-settings")
            .WithTags("Portal Knowledge Settings");

        portal.MapGet(string.Empty, async (int tenantId, ITenantKnowledgeConfigurationService service, CancellationToken cancellationToken) =>
        {
            if (tenantId <= 0)
                return Results.BadRequest("Tenant id must be greater than zero.");

            var result = await service.GetActiveAsync(tenantId, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        portal.MapGet("/setup-status", async (int tenantId, ITenantKnowledgeConfigurationSetupService service, CancellationToken cancellationToken) =>
        {
            if (tenantId <= 0)
                return Results.BadRequest("Tenant id must be greater than zero.");

            var result = await service.GetStatusAsync(tenantId, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        portal.MapPost("/default", async (int tenantId, ITenantKnowledgeConfigurationService service, CancellationToken cancellationToken) =>
        {
            if (tenantId <= 0)
                return Results.BadRequest("Tenant id must be greater than zero.");

            try
            {
                var result = await service.CreateDefaultAsync(
                    new CreateDefaultTenantKnowledgeConfigurationCommand(tenantId),
                    cancellationToken);

                return Results.Created($"/api/portal/tenants/{tenantId}/knowledge-settings", result);
            }
            catch (Exception ex) when (IsValidationException(ex))
            {
                return Results.BadRequest(ex.Message);
            }
        });

        portal.MapPut(string.Empty, async (int tenantId, UpdatePortalTenantKnowledgeSettingsRequest request, ITenantKnowledgeConfigurationService service, CancellationToken cancellationToken) =>
        {
            if (tenantId <= 0)
                return Results.BadRequest("Tenant id must be greater than zero.");

            try
            {
                var current = await service.GetActiveAsync(tenantId, cancellationToken)
                              ?? await service.CreateDefaultAsync(
                                  new CreateDefaultTenantKnowledgeConfigurationCommand(tenantId),
                                  cancellationToken);

                var result = await service.UpdateAsync(
                    new UpdateTenantKnowledgeConfigurationCommand(
                        tenantId,
                        current.Id,
                        request.SystemPrompt,
                        request.AssistantInstructionPrompt,
                        request.ChunkSize,
                        request.ChunkOverlap,
                        request.TopKRetrievalCount,
                        request.MaximumChunksInFinalContext,
                        request.MinimumSimilarityThreshold,
                        request.AllowedFileTypes,
                        request.MaximumFileSizeBytes,
                        request.AutoProcessOnUpload,
                        request.ManualApprovalRequiredBeforeIndexing,
                        request.VersioningEnabled),
                    cancellationToken);

                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (Exception ex) when (IsValidationException(ex))
            {
                return Results.BadRequest(ex.Message);
            }
        });
    }

    private static bool IsValidationException(Exception exception)
        => exception is InvalidFieldException or ArgumentException or ArgumentOutOfRangeException;
}

public record UpdatePortalTenantKnowledgeSettingsRequest(
    string SystemPrompt,
    string AssistantInstructionPrompt,
    int ChunkSize,
    int ChunkOverlap,
    int TopKRetrievalCount,
    int MaximumChunksInFinalContext,
    decimal MinimumSimilarityThreshold,
    IReadOnlyList<string> AllowedFileTypes,
    long MaximumFileSizeBytes,
    bool AutoProcessOnUpload,
    bool ManualApprovalRequiredBeforeIndexing,
    bool VersioningEnabled);

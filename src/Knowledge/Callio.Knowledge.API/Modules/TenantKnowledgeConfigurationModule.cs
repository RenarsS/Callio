using Carter;
using Callio.Core.Domain.Exceptions;
using Callio.Knowledge.API.Modules.Requests;
using Callio.Knowledge.Application.KnowledgeConfigurations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Callio.Knowledge.API.Modules;

public class TenantKnowledgeConfigurationModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/internal/tenants/{tenantId:int}/knowledge-configurations")
            .WithTags("Tenant Knowledge Configurations");

        group.MapPost("/default", async (int tenantId, ITenantKnowledgeConfigurationService service, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.CreateDefaultAsync(
                    new CreateDefaultTenantKnowledgeConfigurationCommand(tenantId),
                    cancellationToken);

                return Results.Created($"/api/internal/tenants/{tenantId}/knowledge-configurations/{result.Id}", result);
            }
            catch (Exception ex) when (IsValidationException(ex))
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/active", async (int tenantId, ITenantKnowledgeConfigurationService service, CancellationToken cancellationToken) =>
        {
            var result = await service.GetActiveAsync(tenantId, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapGet("/{configurationId:int}", async (int tenantId, int configurationId, ITenantKnowledgeConfigurationService service, CancellationToken cancellationToken) =>
        {
            var result = await service.GetByIdAsync(tenantId, configurationId, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapPut("/{configurationId:int}", async (int tenantId, int configurationId, UpdateTenantKnowledgeConfigurationRequest request, ITenantKnowledgeConfigurationService service, CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.UpdateAsync(
                    new UpdateTenantKnowledgeConfigurationCommand(
                        tenantId,
                        configurationId,
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

        group.MapPost("/{configurationId:int}/activate", async (int tenantId, int configurationId, ITenantKnowledgeConfigurationService service, CancellationToken cancellationToken) =>
        {
            var result = await service.ActivateAsync(
                new ChangeTenantKnowledgeConfigurationStatusCommand(tenantId, configurationId),
                cancellationToken);

            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapPost("/{configurationId:int}/deactivate", async (int tenantId, int configurationId, ITenantKnowledgeConfigurationService service, CancellationToken cancellationToken) =>
        {
            var result = await service.DeactivateAsync(
                new ChangeTenantKnowledgeConfigurationStatusCommand(tenantId, configurationId),
                cancellationToken);

            return result is null ? Results.NotFound() : Results.Ok(result);
        });
    }

    private static bool IsValidationException(Exception exception)
        => exception is InvalidFieldException or ArgumentException or ArgumentOutOfRangeException;
}

using Carter;
using Callio.Core.Domain.Exceptions;
using Callio.Generation.Application.Generation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Callio.Generation.API.Modules;

public class TenantGenerationModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/portal/tenants/{tenantId:int}/generation")
            .WithTags("Tenant Generation");

        group.MapGet("/prompts", async (
            ITenantGenerationService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetPromptTemplatesAsync(cancellationToken);
            return Results.Ok(result);
        });

        group.MapPost("/responses", async (
            int tenantId,
            GenerateTenantResponseRequest request,
            ITenantGenerationService service,
            CancellationToken cancellationToken) =>
        {
            if (tenantId <= 0)
                return Results.BadRequest("Tenant id must be greater than zero.");

            try
            {
                var result = await service.GenerateAsync(
                    new GenerateTenantResponseCommand(
                        tenantId,
                        request.Input,
                        request.PromptKey,
                        request.DataSources?.Select(MapDataSource).ToList() ?? [],
                        request.Variables ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                        request.SaveResponse ?? true,
                        request.RequestedByUserId,
                        request.RequestedByDisplayName),
                    cancellationToken);

                if (result.Id is null or <= 0)
                    return Results.Ok(result);

                return Results.Created($"/api/portal/tenants/{tenantId}/generation/responses/{result.Id}", result);
            }
            catch (Exception ex) when (IsValidationException(ex))
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/responses", async (
            int tenantId,
            int? take,
            ITenantGenerationService service,
            CancellationToken cancellationToken) =>
        {
            if (tenantId <= 0)
                return Results.BadRequest("Tenant id must be greater than zero.");

            try
            {
                var result = await service.GetResponsesAsync(
                    tenantId,
                    new GetTenantGenerationResponsesQuery(take),
                    cancellationToken);

                return Results.Ok(result);
            }
            catch (Exception ex) when (IsValidationException(ex))
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/responses/{responseId:int}", async (
            int tenantId,
            int responseId,
            ITenantGenerationService service,
            CancellationToken cancellationToken) =>
        {
            if (tenantId <= 0 || responseId <= 0)
                return Results.BadRequest("Tenant id and response id must be greater than zero.");

            try
            {
                var result = await service.GetResponseAsync(tenantId, responseId, cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (Exception ex) when (IsValidationException(ex))
            {
                return Results.BadRequest(ex.Message);
            }
        });
    }

    private static GenerationDataSourceSelectionDto MapDataSource(GenerationDataSourceRequest request)
        => new(
            string.IsNullOrWhiteSpace(request.SourceKind) ? "KnowledgeChunk" : request.SourceKind,
            request.CategoryId,
            request.CategoryName,
            request.TagId,
            request.TagName,
            request.DocumentId,
            request.MaxChunks,
            request.IncludeBlobContent);

    private static bool IsValidationException(Exception exception)
        => exception is InvalidFieldException or ArgumentException or ArgumentOutOfRangeException or InvalidOperationException or NotSupportedException;
}

public record GenerateTenantResponseRequest(
    string Input,
    string? PromptKey,
    IReadOnlyList<GenerationDataSourceRequest>? DataSources,
    IReadOnlyDictionary<string, string>? Variables,
    bool? SaveResponse,
    string? RequestedByUserId,
    string? RequestedByDisplayName);

public record GenerationDataSourceRequest(
    string? SourceKind,
    int? CategoryId,
    string? CategoryName,
    int? TagId,
    string? TagName,
    int? DocumentId,
    int? MaxChunks,
    bool IncludeBlobContent);

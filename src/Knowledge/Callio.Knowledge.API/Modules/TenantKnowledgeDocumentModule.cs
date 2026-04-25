using Carter;
using Callio.Core.Domain.Exceptions;
using Callio.Knowledge.Application.KnowledgeDocuments;
using Callio.Knowledge.Domain.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Callio.Knowledge.API.Modules;

public class TenantKnowledgeDocumentModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var portal = app.MapGroup("api/portal/tenants/{tenantId:int}/knowledge")
            .WithTags("Tenant Knowledge Documents");

        portal.MapGet("/documents", async (
            int tenantId,
            int? categoryId,
            int? tagId,
            string? status,
            ITenantKnowledgeDocumentService service,
            CancellationToken cancellationToken) =>
        {
            if (tenantId <= 0)
                return Results.BadRequest("Tenant id must be greater than zero.");

            try
            {
                var result = await service.GetDocumentsAsync(
                    tenantId,
                    new GetTenantKnowledgeDocumentsQuery(categoryId, tagId, status),
                    cancellationToken);

                return Results.Ok(result);
            }
            catch (Exception ex) when (IsValidationException(ex))
            {
                return Results.BadRequest(ex.Message);
            }
        });

        portal.MapGet("/documents/{documentId:int}", async (
            int tenantId,
            int documentId,
            ITenantKnowledgeDocumentService service,
            CancellationToken cancellationToken) =>
        {
            if (tenantId <= 0 || documentId <= 0)
                return Results.BadRequest("Tenant id and document id must be greater than zero.");

            var result = await service.GetByIdAsync(tenantId, documentId, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        portal.MapPost("/documents/upload", async (
            int tenantId,
            HttpRequest request,
            ITenantKnowledgeDocumentService service,
            CancellationToken cancellationToken) =>
        {
            if (tenantId <= 0)
                return Results.BadRequest("Tenant id must be greater than zero.");

            try
            {
                var command = await CreateUploadCommandAsync(
                    tenantId,
                    KnowledgeDocumentSourceType.ManualUpload,
                    request,
                    cancellationToken);

                var result = await service.UploadAsync(command, cancellationToken);
                return Results.Created($"/api/portal/tenants/{tenantId}/knowledge/documents/{result.Id}", result);
            }
            catch (Exception ex) when (IsValidationException(ex))
            {
                return Results.BadRequest(ex.Message);
            }
        });

        portal.MapGet("/categories", async (
            int tenantId,
            ITenantKnowledgeDocumentService service,
            CancellationToken cancellationToken) =>
        {
            if (tenantId <= 0)
                return Results.BadRequest("Tenant id must be greater than zero.");

            return Results.Ok(await service.GetCategoriesAsync(tenantId, cancellationToken));
        });

        portal.MapPost("/categories", async (
            int tenantId,
            CreateKnowledgeCategoryRequest request,
            ITenantKnowledgeDocumentService service,
            CancellationToken cancellationToken) =>
        {
            if (tenantId <= 0)
                return Results.BadRequest("Tenant id must be greater than zero.");

            try
            {
                var result = await service.CreateCategoryAsync(
                    new CreateTenantKnowledgeCategoryCommand(tenantId, request.Name, request.Description),
                    cancellationToken);

                return Results.Created($"/api/portal/tenants/{tenantId}/knowledge/categories/{result.Id}", result);
            }
            catch (Exception ex) when (IsValidationException(ex))
            {
                return Results.BadRequest(ex.Message);
            }
        });

        portal.MapGet("/tags", async (
            int tenantId,
            ITenantKnowledgeDocumentService service,
            CancellationToken cancellationToken) =>
        {
            if (tenantId <= 0)
                return Results.BadRequest("Tenant id must be greater than zero.");

            return Results.Ok(await service.GetTagsAsync(tenantId, cancellationToken));
        });

        portal.MapPost("/tags", async (
            int tenantId,
            CreateKnowledgeTagRequest request,
            ITenantKnowledgeDocumentService service,
            CancellationToken cancellationToken) =>
        {
            if (tenantId <= 0)
                return Results.BadRequest("Tenant id must be greater than zero.");

            try
            {
                var result = await service.CreateTagAsync(
                    new CreateTenantKnowledgeTagCommand(tenantId, request.Name),
                    cancellationToken);

                return Results.Created($"/api/portal/tenants/{tenantId}/knowledge/tags/{result.Id}", result);
            }
            catch (Exception ex) when (IsValidationException(ex))
            {
                return Results.BadRequest(ex.Message);
            }
        });

        var publicApi = app.MapGroup("api/public/knowledge")
            .WithTags("Tenant Knowledge Public Upload");

        publicApi.MapPost("/documents/upload", async (
            HttpRequest request,
            ITenantKnowledgeDocumentService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var tenantKeyValue = request.Headers["X-Tenant-Key"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(tenantKeyValue))
                {
                    var form = await request.ReadFormAsync(cancellationToken);
                    tenantKeyValue = form["tenantKey"].FirstOrDefault();
                }

                if (!Guid.TryParse(tenantKeyValue, out var tenantKey))
                    return Results.BadRequest("A valid tenant key is required.");

                var command = await CreateUploadCommandAsync(
                    tenantId: 0,
                    KnowledgeDocumentSourceType.TenantApi,
                    request,
                    cancellationToken);

                var result = await service.UploadByTenantKeyAsync(tenantKey, command, cancellationToken);
                return Results.Created($"/api/portal/tenants/{result.TenantId}/knowledge/documents/{result.Id}", result);
            }
            catch (Exception ex) when (IsValidationException(ex))
            {
                return Results.BadRequest(ex.Message);
            }
        });
    }

    private static async Task<UploadTenantKnowledgeDocumentCommand> CreateUploadCommandAsync(
        int tenantId,
        KnowledgeDocumentSourceType sourceType,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files["file"] ?? form.Files.FirstOrDefault();
        if (file is null || file.Length <= 0)
            throw new InvalidOperationException("A file upload is required.");

        await using var stream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);

        return new UploadTenantKnowledgeDocumentCommand(
            tenantId,
            form["title"].FirstOrDefault(),
            file.FileName,
            file.ContentType,
            memoryStream.ToArray(),
            ParseNullableInt(form["categoryId"].FirstOrDefault()),
            form["categoryName"].FirstOrDefault(),
            ParseIntValues(form, "tagIds"),
            ParseStringValues(form, "tagNames"),
            form["uploadedByUserId"].FirstOrDefault(),
            form["uploadedByDisplayName"].FirstOrDefault(),
            ParseBoolean(form["approveForIndexing"].FirstOrDefault(), defaultValue: sourceType == KnowledgeDocumentSourceType.ManualUpload),
            sourceType);
    }

    private static IReadOnlyList<int> ParseIntValues(IFormCollection form, string key)
        => form[key]
            .SelectMany(value => SplitList(value))
            .Select(value => int.TryParse(value, out var parsed) ? parsed : -1)
            .Where(value => value > 0)
            .Distinct()
            .ToList();

    private static IReadOnlyList<string> ParseStringValues(IFormCollection form, string key)
        => form[key]
            .SelectMany(value => SplitList(value))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static IEnumerable<string> SplitList(string? value)
        => (value ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static int? ParseNullableInt(string? value)
        => int.TryParse(value, out var parsed) && parsed > 0 ? parsed : null;

    private static bool ParseBoolean(string? value, bool defaultValue)
        => bool.TryParse(value, out var parsed) ? parsed : defaultValue;

    private static bool IsValidationException(Exception exception)
        => exception is InvalidFieldException or ArgumentException or ArgumentOutOfRangeException or InvalidOperationException or NotSupportedException;
}

public record CreateKnowledgeCategoryRequest(string Name, string? Description);

public record CreateKnowledgeTagRequest(string Name);

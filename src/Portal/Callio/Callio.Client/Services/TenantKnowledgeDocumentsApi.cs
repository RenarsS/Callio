using System.Net.Http.Json;
using Callio.Client.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Callio.Client.Services;

public class TenantKnowledgeDocumentsApi(HttpClient httpClient)
{
    public async Task<IReadOnlyList<PortalKnowledgeDocumentResponse>> GetDocumentsAsync(int tenantId, CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<List<PortalKnowledgeDocumentResponse>>(
               $"/api/portal/tenants/{tenantId}/knowledge/documents",
               cancellationToken)
           ?? [];

    public async Task<IReadOnlyList<PortalKnowledgeCategoryResponse>> GetCategoriesAsync(int tenantId, CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<List<PortalKnowledgeCategoryResponse>>(
               $"/api/portal/tenants/{tenantId}/knowledge/categories",
               cancellationToken)
           ?? [];

    public async Task<IReadOnlyList<PortalKnowledgeTagResponse>> GetTagsAsync(int tenantId, CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<List<PortalKnowledgeTagResponse>>(
               $"/api/portal/tenants/{tenantId}/knowledge/tags",
               cancellationToken)
           ?? [];

    public async Task<PortalKnowledgeDocumentResponse> UploadAsync(
        int tenantId,
        PortalTenantKnowledgeDocumentUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        await using var stream = request.File.OpenReadStream(request.File.Size, cancellationToken);
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(request.File.ContentType) ? "application/octet-stream" : request.File.ContentType);

        form.Add(fileContent, "file", request.File.Name);

        AddStringIfPresent(form, "title", request.Title);
        AddStringIfPresent(form, "categoryId", request.CategoryId is > 0 ? request.CategoryId.Value.ToString() : null);
        AddStringIfPresent(form, "categoryName", request.CategoryName);
        AddStringIfPresent(form, "uploadedByUserId", request.UploadedByUserId);
        AddStringIfPresent(form, "uploadedByDisplayName", request.UploadedByDisplayName);
        AddStringIfPresent(form, "approveForIndexing", request.ApproveForIndexing.ToString());

        foreach (var tagName in request.TagNames.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            form.Add(new StringContent(tagName.Trim()), "tagNames");
        }

        var response = await httpClient.PostAsync(
            $"/api/portal/tenants/{tenantId}/knowledge/documents/upload",
            form,
            cancellationToken);

        await PortalApiResponseHelper.EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<PortalKnowledgeDocumentResponse>(cancellationToken)
               ?? throw new InvalidOperationException("Knowledge document response was empty.");
    }

    private static void AddStringIfPresent(MultipartFormDataContent form, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        form.Add(new StringContent(value.Trim()), name);
    }
}

public record PortalTenantKnowledgeDocumentUploadRequest(
    IBrowserFile File,
    string? Title,
    int? CategoryId,
    string? CategoryName,
    IReadOnlyList<string> TagNames,
    string? UploadedByUserId,
    string? UploadedByDisplayName,
    bool ApproveForIndexing);

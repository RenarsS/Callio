using System.Net;
using System.Net.Http.Json;
using Callio.Client.Models;

namespace Callio.Client.Services;

public class TenantKnowledgeSettingsApi(HttpClient httpClient)
{
    public async Task<PortalTenantKnowledgeSettingsResponse?> GetSettingsAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"/api/portal/tenants/{tenantId}/knowledge-settings", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await PortalApiResponseHelper.EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<PortalTenantKnowledgeSettingsResponse>(cancellationToken)
               ?? throw new InvalidOperationException("Knowledge settings response was empty.");
    }

    public async Task<PortalTenantKnowledgeSetupStatusResponse?> GetSetupStatusAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"/api/portal/tenants/{tenantId}/knowledge-settings/setup-status", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await PortalApiResponseHelper.EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<PortalTenantKnowledgeSetupStatusResponse>(cancellationToken)
               ?? throw new InvalidOperationException("Knowledge settings setup status response was empty.");
    }

    public async Task<PortalTenantKnowledgeSettingsResponse> CreateDefaultAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync($"/api/portal/tenants/{tenantId}/knowledge-settings/default", content: null, cancellationToken);
        await PortalApiResponseHelper.EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<PortalTenantKnowledgeSettingsResponse>(cancellationToken)
               ?? throw new InvalidOperationException("Knowledge settings response was empty.");
    }

    public async Task<PortalTenantKnowledgeSettingsResponse> SaveAsync(int tenantId, UpdatePortalTenantKnowledgeSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/api/portal/tenants/{tenantId}/knowledge-settings", request, cancellationToken);
        await PortalApiResponseHelper.EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<PortalTenantKnowledgeSettingsResponse>(cancellationToken)
               ?? throw new InvalidOperationException("Knowledge settings response was empty.");
    }
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

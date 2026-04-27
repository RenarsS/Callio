using System.Net.Http.Json;
using Callio.Client.Models;

namespace Callio.Client.Services;

public class TenantGenerationApi(HttpClient httpClient)
{
    public async Task<IReadOnlyList<PortalGenerationPromptResponse>> GetPromptsAsync(int tenantId, CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<List<PortalGenerationPromptResponse>>(
               $"/api/portal/tenants/{tenantId}/generation/prompts",
               cancellationToken)
           ?? [];

    public async Task<PortalGenerationPromptResponse> CreatePromptAsync(
        int tenantId,
        SavePortalGenerationPromptRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"/api/portal/tenants/{tenantId}/generation/prompts",
            request,
            cancellationToken);

        await PortalApiResponseHelper.EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<PortalGenerationPromptResponse>(cancellationToken)
               ?? throw new InvalidOperationException("Generation prompt response was empty.");
    }

    public async Task<PortalGenerationPromptResponse> UpdatePromptAsync(
        int tenantId,
        int promptTemplateId,
        SavePortalGenerationPromptRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync(
            $"/api/portal/tenants/{tenantId}/generation/prompts/{promptTemplateId}",
            request,
            cancellationToken);

        await PortalApiResponseHelper.EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<PortalGenerationPromptResponse>(cancellationToken)
               ?? throw new InvalidOperationException("Generation prompt response was empty.");
    }

    public async Task<IReadOnlyList<PortalGenerationResponse>> GetResponsesAsync(
        int tenantId,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        var suffix = take.HasValue ? $"?take={take.Value}" : string.Empty;
        return await httpClient.GetFromJsonAsync<List<PortalGenerationResponse>>(
                   $"/api/portal/tenants/{tenantId}/generation/responses{suffix}",
                   cancellationToken)
               ?? [];
    }

    public async Task<PortalGenerationResponse?> GetResponseAsync(
        int tenantId,
        int responseId,
        CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<PortalGenerationResponse>(
            $"/api/portal/tenants/{tenantId}/generation/responses/{responseId}",
            cancellationToken);
}

public record SavePortalGenerationPromptRequest(
    string Key,
    string Name,
    string? Description,
    string SystemPrompt,
    string UserPromptTemplate,
    IReadOnlyList<PortalGenerationDataSourceResponse>? DataSources);

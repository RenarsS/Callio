using Callio.Generation.Domain;

namespace Callio.Generation.Application.Generation;

public interface ITenantGenerationRepository
{
    Task<IReadOnlyList<TenantGenerationPromptTemplate>> GetPromptTemplatesAsync(
        int tenantId,
        CancellationToken cancellationToken = default);

    Task<TenantGenerationPromptTemplate?> GetPromptTemplateByIdAsync(
        int tenantId,
        int promptTemplateId,
        CancellationToken cancellationToken = default);

    Task<TenantGenerationPromptTemplate?> GetPromptTemplateByKeyAsync(
        int tenantId,
        string promptKey,
        CancellationToken cancellationToken = default);

    Task<TenantGenerationPromptTemplate> AddPromptTemplateAsync(
        TenantGenerationPromptTemplate promptTemplate,
        CancellationToken cancellationToken = default);

    Task<TenantGenerationPromptTemplate> UpdatePromptTemplateAsync(
        TenantGenerationPromptTemplate promptTemplate,
        CancellationToken cancellationToken = default);

    Task<TenantGenerationResponse> AddAsync(
        TenantGenerationResponse response,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantGenerationResponse>> GetRecentAsync(
        int tenantId,
        int take,
        CancellationToken cancellationToken = default);

    Task<TenantGenerationResponse?> GetByIdAsync(
        int tenantId,
        int responseId,
        CancellationToken cancellationToken = default);
}

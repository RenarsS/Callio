using Callio.Generation.Domain;

namespace Callio.Generation.Application.Generation;

public interface ITenantGenerationRepository
{
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

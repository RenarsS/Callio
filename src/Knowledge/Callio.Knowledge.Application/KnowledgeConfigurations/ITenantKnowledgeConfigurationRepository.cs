using Callio.Knowledge.Domain;

namespace Callio.Knowledge.Application.KnowledgeConfigurations;

public interface ITenantKnowledgeConfigurationRepository
{
    Task<TenantKnowledgeConfiguration?> GetByIdAsync(int tenantId, int configurationId, CancellationToken cancellationToken = default);

    Task<TenantKnowledgeConfiguration?> GetActiveAsync(int tenantId, CancellationToken cancellationToken = default);

    Task<TenantKnowledgeConfiguration?> GetLatestAsync(int tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, TenantKnowledgeConfiguration>> GetActiveByTenantIdsAsync(
        IReadOnlyCollection<int> tenantIds,
        CancellationToken cancellationToken = default);

    Task<TenantKnowledgeConfiguration> AddAsync(TenantKnowledgeConfiguration configuration, CancellationToken cancellationToken = default);

    Task<TenantKnowledgeConfiguration> UpdateAsync(TenantKnowledgeConfiguration configuration, CancellationToken cancellationToken = default);

    Task<TenantKnowledgeConfiguration?> ActivateAsync(int tenantId, int configurationId, DateTime now, CancellationToken cancellationToken = default);

    Task<TenantKnowledgeConfiguration?> DeactivateAsync(int tenantId, int configurationId, DateTime now, CancellationToken cancellationToken = default);
}

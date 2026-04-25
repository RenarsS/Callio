namespace Callio.Knowledge.Application.KnowledgeConfigurations;

public interface ITenantKnowledgeConfigurationService
{
    Task<TenantKnowledgeConfigurationDto> CreateDefaultAsync(
        CreateDefaultTenantKnowledgeConfigurationCommand command,
        CancellationToken cancellationToken = default);

    Task<TenantKnowledgeConfigurationDto?> GetActiveAsync(int tenantId, CancellationToken cancellationToken = default);

    Task<TenantKnowledgeConfigurationDto?> GetByIdAsync(int tenantId, int configurationId, CancellationToken cancellationToken = default);

    Task<TenantKnowledgeConfigurationDto?> UpdateAsync(
        UpdateTenantKnowledgeConfigurationCommand command,
        CancellationToken cancellationToken = default);

    Task<TenantKnowledgeConfigurationDto?> ActivateAsync(
        ChangeTenantKnowledgeConfigurationStatusCommand command,
        CancellationToken cancellationToken = default);

    Task<TenantKnowledgeConfigurationDto?> DeactivateAsync(
        ChangeTenantKnowledgeConfigurationStatusCommand command,
        CancellationToken cancellationToken = default);
}

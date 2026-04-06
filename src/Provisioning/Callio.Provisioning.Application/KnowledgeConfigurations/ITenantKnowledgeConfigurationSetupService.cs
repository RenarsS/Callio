namespace Callio.Provisioning.Application.KnowledgeConfigurations;

public interface ITenantKnowledgeConfigurationSetupService
{
    Task EnsurePendingAsync(int tenantId, CancellationToken cancellationToken = default);

    Task<TenantKnowledgeConfigurationSetupStatusDto> HandleProvisioningSucceededAsync(
        RunTenantKnowledgeConfigurationSetupCommand command,
        CancellationToken cancellationToken = default);

    Task<TenantKnowledgeConfigurationSetupStatusDto?> RetryAsync(
        RunTenantKnowledgeConfigurationSetupCommand command,
        CancellationToken cancellationToken = default);

    Task<TenantKnowledgeConfigurationSetupStatusDto?> GetStatusAsync(int tenantId, CancellationToken cancellationToken = default);
}

namespace Callio.Provisioning.Application;

public interface ITenantProvisioningService
{
    Task<TenantProvisioningStatusDto> HandleTenantApprovedAsync(
        TenantApprovedProvisioningCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantProvisioningStatusDto>> GetAllStatusesAsync(CancellationToken cancellationToken = default);

    Task<TenantProvisioningStatusDto?> GetStatusAsync(int tenantId, CancellationToken cancellationToken = default);

    Task<TenantProvisioningStatusDto?> RetryFailedAsync(int tenantId, CancellationToken cancellationToken = default);

    Task<TenantProvisioningStatusDto?> ReprovisionAsync(int tenantId, CancellationToken cancellationToken = default);
}

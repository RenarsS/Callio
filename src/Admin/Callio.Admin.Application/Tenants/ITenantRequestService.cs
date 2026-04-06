namespace Callio.Admin.Application.Tenants;

public interface ITenantRequestService
{
    Task<PortalTenantOnboardingResultDto> RegisterPortalUserAndRequestTenantAsync(RegisterPortalUserAndTenantCommand command, CancellationToken cancellationToken = default);
    Task<PortalTenantRequestStatusDto?> GetPortalStatusAsync(int requestId, string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantRequestListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TenantRequestListItemDto?> ApproveAsync(ProcessTenantRequestCommand command, CancellationToken cancellationToken = default);
    Task<TenantRequestListItemDto?> RejectAsync(ProcessTenantRequestCommand command, CancellationToken cancellationToken = default);
}

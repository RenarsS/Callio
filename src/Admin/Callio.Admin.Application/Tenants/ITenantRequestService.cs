namespace Callio.Admin.Application.Tenants;

public interface ITenantRequestService
{
    Task<TenantRequestListItemDto> CreateAsync(CreateTenantRequestCommand command, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantRequestListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TenantRequestListItemDto?> ApproveAsync(ProcessTenantRequestCommand command, CancellationToken cancellationToken = default);
    Task<TenantRequestListItemDto?> RejectAsync(ProcessTenantRequestCommand command, CancellationToken cancellationToken = default);
}
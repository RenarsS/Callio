using System.Security.Claims;

namespace Callio.Core.Domain.Identity;

public sealed record PortalUserContext(
    string UserId,
    string Email,
    string DisplayName,
    string UserType,
    int? TenantId);

public interface IPortalUserContextAccessor
{
    Task<PortalUserContext?> GetCurrentAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}

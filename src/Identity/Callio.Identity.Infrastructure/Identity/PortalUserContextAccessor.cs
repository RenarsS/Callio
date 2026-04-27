using System.Security.Claims;
using Callio.Core.Domain.Constants.Identity;
using Callio.Core.Domain.Identity;
using Callio.Identity.Domain;
using Microsoft.AspNetCore.Identity;

namespace Callio.Identity.Infrastructure.Identity;

public class PortalUserContextAccessor(
    UserManager<ApplicationUser> userManager) : IPortalUserContextAccessor
{
    public async Task<PortalUserContext?> GetCurrentAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        if (principal?.Identity?.IsAuthenticated != true)
            return null;

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? principal.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
            return null;

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return null;

        var email = principal.FindFirstValue(ClaimTypes.Email) ?? user.Email ?? string.Empty;
        var displayName = principal.FindFirstValue(AppClaims.DisplayName)
                          ?? principal.FindFirstValue(ClaimTypes.Name)
                          ?? $"{user.FirstName} {user.LastName}".Trim();
        var userType = principal.FindFirstValue(AppClaims.UserType) ?? user.Type.ToString();

        var tenantIdClaimValue = principal.FindFirstValue(AppClaims.TenantId);
        var tenantId = int.TryParse(tenantIdClaimValue, out var parsedTenantId)
            ? parsedTenantId
            : user.TenantId;

        return new PortalUserContext(
            user.Id,
            email,
            string.IsNullOrWhiteSpace(displayName) ? email : displayName,
            userType,
            tenantId);
    }
}

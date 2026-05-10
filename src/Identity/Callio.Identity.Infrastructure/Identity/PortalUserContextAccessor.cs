using System.Security.Claims;
using Callio.Core.Domain.Constants.Identity;
using Callio.Core.Domain.Identity;
using Callio.Identity.Domain;
using Microsoft.AspNetCore.Identity;

namespace Callio.Identity.Infrastructure.Identity;

public class PortalUserContextAccessor(
    UserManager<ApplicationUser> userManager) : IPortalUserContextAccessor
{
    public Task<PortalUserContext?> GetCurrentAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        if (principal?.Identity?.IsAuthenticated != true)
            return Task.FromResult<PortalUserContext?>(null);

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? principal.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
            return Task.FromResult<PortalUserContext?>(null);

        var displayName = principal.FindFirstValue(AppClaims.DisplayName)
                          ?? principal.FindFirstValue(ClaimTypes.Name);
        var userType = principal.FindFirstValue(AppClaims.UserType);
        var email = principal.FindFirstValue(ClaimTypes.Email)
                    ?? principal.FindFirstValue(ClaimTypes.Name);

        var tenantIdClaimValue = principal.FindFirstValue(AppClaims.TenantId);
        int? tenantId = int.TryParse(tenantIdClaimValue, out var parsedTenantId)
            ? parsedTenantId
            : null;

        if (!string.IsNullOrWhiteSpace(email) &&
            !string.IsNullOrWhiteSpace(displayName) &&
            !string.IsNullOrWhiteSpace(userType))
        {
            return Task.FromResult<PortalUserContext?>(new PortalUserContext(
                userId,
                email,
                displayName,
                userType,
                tenantId));
        }

        return GetCurrentFromUserStoreAsync(userId, email, displayName, userType, tenantId);
    }

    private async Task<PortalUserContext?> GetCurrentFromUserStoreAsync(
        string userId,
        string? email,
        string? displayName,
        string? userType,
        int? tenantId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return null;

        email ??= user.Email ?? string.Empty;
        displayName ??= $"{user.FirstName} {user.LastName}".Trim();
        userType ??= user.Type.ToString();
        tenantId ??= user.TenantId;

        return new PortalUserContext(
            user.Id,
            email,
            string.IsNullOrWhiteSpace(displayName) ? email : displayName,
            userType,
            tenantId);
    }
}

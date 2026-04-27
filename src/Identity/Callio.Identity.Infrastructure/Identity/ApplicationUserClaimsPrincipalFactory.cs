using System.Security.Claims;
using Callio.Core.Domain.Constants.Identity;
using Callio.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Callio.Identity.Infrastructure.Identity;

public class ApplicationUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<ApplicationUser>(userManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        identity.AddClaim(new Claim(AppClaims.UserType, user.Type.ToString()));

        if (!string.IsNullOrWhiteSpace(user.FirstName))
            identity.AddClaim(new Claim(ClaimTypes.GivenName, user.FirstName));

        if (!string.IsNullOrWhiteSpace(user.LastName))
            identity.AddClaim(new Claim(ClaimTypes.Surname, user.LastName));

        var displayName = $"{user.FirstName} {user.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            identity.AddClaim(new Claim(AppClaims.DisplayName, displayName));

            if (!identity.HasClaim(claim => claim.Type == ClaimTypes.Name))
                identity.AddClaim(new Claim(ClaimTypes.Name, displayName));
        }

        if (user.TenantId.HasValue)
            identity.AddClaim(new Claim(AppClaims.TenantId, user.TenantId.Value.ToString()));

        return identity;
    }
}

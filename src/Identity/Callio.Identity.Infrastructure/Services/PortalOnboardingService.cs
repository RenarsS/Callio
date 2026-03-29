using Callio.Admin.Application.Tenants;
using Callio.Identity.Application.PortalOnboarding;
using Callio.Identity.Domain;
using Microsoft.AspNetCore.Identity;

namespace Callio.Identity.Infrastructure.Services;

public class PortalOnboardingService(
    UserManager<ApplicationUser> userManager,
    ITenantRequestService tenantRequestService) : IPortalOnboardingService
{
    public async Task<PortalTenantRegistrationResultDto> RegisterPortalUserAndRequestTenantAsync(
        RegisterPortalUserAndTenantCommand command,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await userManager.FindByEmailAsync(command.Email);
        if (existingUser is not null)
            throw new InvalidOperationException("A user with this email already exists.");

        var user = ApplicationUser.CreatePowerUser(command.Email, command.FirstName, command.LastName);

        var createUserResult = await userManager.CreateAsync(user, command.Password);
        if (!createUserResult.Succeeded)
        {
            var errors = string.Join("; ", createUserResult.Errors.Select(x => x.Description));
            throw new InvalidOperationException($"Portal user registration failed: {errors}");
        }

        try
        {
            var tenantRequest = await tenantRequestService.CreateAsync(
                new CreateTenantRequestCommand(
                    command.TenantName,
                    user.Id,
                    command.Email,
                    command.FirstName,
                    command.LastName,
                    command.CompanyName,
                    command.Notes),
                cancellationToken);

            return new PortalTenantRegistrationResultDto(
                user.Id,
                tenantRequest.Id,
                command.Email,
                command.TenantName,
                command.CompanyName,
                tenantRequest.Status.ToString(),
                "User registered and tenant creation request submitted for dashboard review.");
        }
        catch
        {
            await userManager.DeleteAsync(user);
            throw;
        }
    }
}

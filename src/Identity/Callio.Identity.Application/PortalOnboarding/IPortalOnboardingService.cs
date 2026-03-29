namespace Callio.Identity.Application.PortalOnboarding;

public interface IPortalOnboardingService
{
    Task<PortalTenantRegistrationResultDto> RegisterPortalUserAndRequestTenantAsync(
        RegisterPortalUserAndTenantCommand command,
        CancellationToken cancellationToken = default);
}
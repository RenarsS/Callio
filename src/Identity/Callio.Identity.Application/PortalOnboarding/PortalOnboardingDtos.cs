namespace Callio.Identity.Application.PortalOnboarding;

public record RegisterPortalUserAndTenantCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string CompanyName,
    string TenantName,
    string? Notes);

public record PortalTenantRegistrationResultDto(
    string UserId,
    int TenantRequestId,
    string Email,
    string TenantName,
    string CompanyName,
    string Status,
    string Message);
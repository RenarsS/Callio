namespace Callio.Identity.API.Contracts.PortalOnboarding;

public record RegisterPortalUserAndTenantRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string CompanyName,
    string TenantName,
    string? Notes);
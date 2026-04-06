namespace Callio.Admin.API.Contracts.Tenants;

public record RegisterPortalUserAndTenantRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string CompanyName,
    string TenantName,
    int? SelectedPlanId,
    string? Notes);

public record ProcessTenantRequestRequest(
    string ProcessedByUserId,
    string? DecisionNote);

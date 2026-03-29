namespace Callio.Admin.API.Contracts.Tenants;

public record CreateTenantRequestRequest(
    string TenantName,
    string RequestedByUserId,
    string RequestedByEmail,
    string RequestedByFirstName,
    string RequestedByLastName,
    string CompanyName,
    string? Notes);

public record ProcessTenantRequestRequest(
    string ProcessedByUserId,
    string? DecisionNote);
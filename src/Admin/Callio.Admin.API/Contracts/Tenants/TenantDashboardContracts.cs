namespace Callio.Admin.API.Contracts.Tenants;

public record TenantListItemResponse(
    int Id,
    string Name,
    Guid? TenantCode,
    string ContactName,
    string ContactEmail,
    DateTime CreatedAt,
    DateTime ActivatedAt,
    DateTime? DeactivatedAt,
    string Status,
    string? CurrentPlanName,
    string? SubscriptionStatus);
namespace Callio.Client.Models;

public record PortalPlanResponse(
    int Id,
    string Name,
    string Description,
    decimal BasePrice,
    string Currency,
    string BillingLabel,
    IReadOnlyList<PortalPlanQuotaResponse> Quotas);

public record PortalPlanQuotaResponse(string Metric, string Limit, bool HardLimit, string? OverageLabel);
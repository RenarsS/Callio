using Callio.Admin.Domain.Enums;

namespace Callio.Admin.API.Contracts.Billing;

public record PlanListItemResponse(
    int Id,
    string Name,
    string Description,
    decimal BasePrice,
    string Currency,
    BillingInterval BillingInterval,
    int AnchorDay,
    bool IsActive,
    IReadOnlyList<PlanQuotaResponse> Quotas);

public record PlanQuotaResponse(
    int Id,
    int UsageMetricId,
    string UsageMetricKey,
    string UsageMetricDisplayName,
    string Unit,
    decimal Limit,
    bool HardLimit,
    decimal? OverageUnitPrice,
    string? OverageCurrency);

public record CreatePlanRequest(
    string Name,
    string Description,
    decimal BasePrice,
    string Currency,
    BillingInterval BillingInterval,
    int AnchorDay,
    bool IsActive);

public record UpdatePlanRequest(
    string Name,
    string Description,
    decimal BasePrice,
    string Currency,
    BillingInterval BillingInterval,
    int AnchorDay,
    bool IsActive);

public record CreatePlanQuotaRequest(
    int UsageMetricId,
    decimal Limit,
    bool HardLimit,
    decimal? OverageUnitPrice,
    string? Currency);

public record UpdatePlanQuotaRequest(
    decimal Limit,
    bool HardLimit,
    decimal? OverageUnitPrice,
    string? Currency);

public record UsageMetricResponse(int Id, string Key, string DisplayName, string Unit, MeasurementType Type);

public record CreateUsageMetricRequest(string Key, string DisplayName, string Unit, MeasurementType Type);

public record UpdateUsageMetricRequest(string Key, string DisplayName, string Unit, MeasurementType Type);

public record PortalPlanResponse(
    int Id,
    string Name,
    string Description,
    decimal BasePrice,
    string Currency,
    string BillingLabel,
    IReadOnlyList<PortalPlanQuotaResponse> Quotas);

public record PortalPlanQuotaResponse(string Metric, string Limit, bool HardLimit, string? OverageLabel);
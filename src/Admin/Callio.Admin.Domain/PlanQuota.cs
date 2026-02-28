using Callio.Admin.Domain.ValueObjects;

namespace Callio.Admin.Domain;

public class PlanQuota(int planId, int usageMetricId, decimal limit, bool hardLimit, Money? overageUnitPrice = null)
{
    public int PlanId { get; private set; } = planId;

    public int UsageMetricId { get; private set; } = usageMetricId;

    public decimal Limit { get; private set; } = limit;

    public bool HardLimit { get; private set; } = hardLimit;

    public Money? OverageUnitPrice { get; private set; } = overageUnitPrice;
    
    public bool IsUnlimited => Limit == -1;
    
    public decimal CalculateOverage(decimal used) => Math.Max(0, used - (Limit == -1 ? used : Limit));
}
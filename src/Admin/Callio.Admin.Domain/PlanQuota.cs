using Callio.Admin.Domain.ValueObjects;
using Callio.Core.Domain.Helpers;

namespace Callio.Admin.Domain;

public class PlanQuota : Entity<int>
{
    public int PlanId { get; private set; }

    public int UsageMetricId { get; private set; }

    public decimal Limit { get; private set; }

    public bool HardLimit { get; private set; }

    public Money? OverageUnitPrice { get; private set; }

    private PlanQuota() { }
    
    public PlanQuota(int planId, int usageMetricId, decimal limit, bool hardLimit, Money? overageUnitPrice = null)
    {
        PlanId = planId;
        UsageMetricId = usageMetricId;
        Limit = limit;
        HardLimit = hardLimit;
        OverageUnitPrice = overageUnitPrice;
    }
    
    public bool IsUnlimited => Limit == -1;
    
    public decimal CalculateOverage(decimal used) => Math.Max(0, used - (Limit == -1 ? used : Limit));
}
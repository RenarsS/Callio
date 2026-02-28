using Callio.Admin.Domain.Enums;
using Callio.Admin.Domain.ValueObjects;
using Callio.Core.Domain.Helpers;

namespace Callio.Admin.Domain;

public class Subscription(int tenantId, int planId, DateRange currentPeriod, DateTime? trialEndsAt = null)
    : Entity<int>
{
    public int TenantId { get; private set; } = tenantId;

    public int PlanId { get; private set; } = planId;

    public SubscriptionStatus Status { get; private set; } = trialEndsAt.HasValue ? SubscriptionStatus.Trial : SubscriptionStatus.Active;

    public DateRange CurrentPeriod { get; private set; } = currentPeriod;

    public DateTime? TrialEndsAt { get; private set; } = trialEndsAt;

    public DateTime? CancelledAt { get; private set; }
    
    public DateTime? CancellationScheduledAt { get; private set; }

    public void Activate()
    {
        Status = SubscriptionStatus.Active;
        TrialEndsAt = null;
    }

    public void Cancel(bool immediately, DateTime? now = null)
    {
        if (immediately)
        {
            Status = SubscriptionStatus.Cancelled;
            CancelledAt = now ?? DateTime.Now;
        }
        else
        {
            CancellationScheduledAt = CurrentPeriod.End;
        }
    }

    public void Renew(DateRange newPeriod)
    {
        CurrentPeriod = newPeriod;
        Status = newPeriod.Contains(newPeriod.Now) ? SubscriptionStatus.Active : SubscriptionStatus.Pending;
    }

    public void MarkPastDue() => Status = SubscriptionStatus.PastDue;
    
    public void Suspend() => Status = SubscriptionStatus.Suspended;

    public void ChangePlan(int newPlanId)
    {
        PlanId = newPlanId;
    }
}
using Callio.Admin.Domain.Enums;
using Callio.Admin.Domain.ValueObjects;
using Callio.Core.Domain.Helpers;

namespace Callio.Admin.Domain;

public class Subscription
    : Entity<int>
{
    public int TenantId { get; private set; }

    public int PlanId { get; private set; }

    public SubscriptionStatus Status { get; private set; }

    public DateRange CurrentPeriod { get; private set; }

    public DateTime? TrialEndsAt { get; private set; }

    public DateTime? CancelledAt { get; private set; }
    
    public DateTime? CancellationScheduledAt { get; private set; }
    
    private Subscription() { }

    public Subscription(int tenantId, int planId, DateRange currentPeriod, DateTime? trialEndsAt = null)
    {
        TenantId = tenantId;
        PlanId = planId;
        CurrentPeriod = currentPeriod;
        TrialEndsAt = trialEndsAt;
        Status = trialEndsAt.HasValue ? SubscriptionStatus.Trial : SubscriptionStatus.Active;
        
    }

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
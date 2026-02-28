using Callio.Admin.Domain.ValueObjects;
using Callio.Core.Domain.Helpers;

namespace Callio.Admin.Domain;

public class Plan(string name, string description, Money basePrice, BillingCycle billingCycle)
    : Entity<int>
{
    public string Name { get; private set; } = name;
    
    public string Description { get; private set; } = description;
    
    public Money BasePrice { get; private set; } = basePrice;
    
    public BillingCycle BillingCycle { get; private set; } = billingCycle;
    
    public bool IsActive { get; private set; } = true;

    private readonly List<PlanQuota> _quotas = new();
    
    public IReadOnlyCollection<PlanQuota> Quotas => _quotas.AsReadOnly();

    public void AddQuota(PlanQuota quota) => _quotas.Add(quota);
    
    public void Deactivate() => IsActive = false;
}
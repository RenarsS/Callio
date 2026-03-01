using Callio.Admin.Domain.ValueObjects;
using Callio.Core.Domain.Helpers;

namespace Callio.Admin.Domain;

public class Plan : Entity<int>
{
    public string Name { get; private set; }
    
    public string Description { get; private set; }
    
    public Money BasePrice { get; private set; }
    
    public BillingCycle BillingCycle { get; private set; }
    
    public bool IsActive { get; private set; } = true;

    private readonly List<PlanQuota> _quotas = new();
    
    public IReadOnlyCollection<PlanQuota> Quotas => _quotas.AsReadOnly();

    private Plan() { }
    
    public Plan(string name, string description, Money basePrice, BillingCycle billingCycle)
    {
        Name = name;
        Description = description;
        BasePrice = basePrice;
        BillingCycle = billingCycle;
    }
    
    public void AddQuota(PlanQuota quota) => _quotas.Add(quota);
    
    public void Deactivate() => IsActive = false;
}
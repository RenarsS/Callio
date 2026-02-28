using Callio.Admin.Domain;
using Callio.Admin.Domain.Enums;
using Callio.Admin.Domain.ValueObjects;
using FluentAssertions;

namespace Callio.Admin.Tests.Domain;

public class PlanTests
{
    [Fact]
    public void Plan_AllFieldsAreValid_FieldsAreSet()
    {
        // Arrange
        var basePrice = new Money(25m, "EUR");
        var billingCycle = new BillingCycle(BillingInterval.Monthly, 8);

        // Act
        var plan = new Plan("Plan", "Description", basePrice, billingCycle);

        // Assert
        plan.Name.Should().Be("Plan");
        plan.Description.Should().Be("Description");
        plan.BasePrice.Amount.Should().Be(25m);
        plan.BasePrice.Currency.Should().Be("EUR");
        plan.BillingCycle.Interval.Should().Be(BillingInterval.Monthly);
        plan.BillingCycle.AnchorDay.Should().Be(8);
    }
    
    [Fact]
    public void AddQuota_AllFieldsAreValid_FieldsAreSet()
    {
        // Arrange
        var basePrice = new Money(25m, "EUR");
        var billingCycle = new BillingCycle(BillingInterval.Monthly, 8);
        var plan = new Plan("Plan", "Description", basePrice, billingCycle);
        var planQuota = new PlanQuota(1, 1, -1, false, new Money((decimal)2.5, "EUR"));

        // Act
        plan.AddQuota(planQuota);

        // Assert
        plan.Quotas.Count.Should().Be(1);
    }
    
    
    [Fact]
    public void Deactivate_AllFieldsAreValid_FieldsAreSet()
    {
        // Arrange
        var basePrice = new Money(25m, "EUR");
        var billingCycle = new BillingCycle(BillingInterval.Monthly, 8);
        var plan = new Plan("Plan", "Description", basePrice, billingCycle);

        // Act
        plan.Deactivate();

        // Assert
        plan.IsActive.Should().BeFalse();
    }
}
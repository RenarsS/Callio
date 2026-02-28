using Callio.Admin.Domain.Enums;
using Callio.Admin.Domain.ValueObjects;
using FluentAssertions;

namespace Callio.Admin.Tests.Domain.ValueObjects;

public class BillingCycleTests
{
    [Fact]
    public void BillingCycle_FieldsValid_FieldsSet()
    {
        // Act
        var billingCycle = new BillingCycle(BillingInterval.Annual, 6);

        // Assert
        billingCycle.Interval.Should().Be(BillingInterval.Annual);
        billingCycle.AnchorDay.Should().Be(6);
    }
}
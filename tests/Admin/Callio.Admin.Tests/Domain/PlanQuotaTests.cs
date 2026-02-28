using Callio.Admin.Domain;
using Callio.Admin.Domain.ValueObjects;
using FluentAssertions;

namespace Callio.Admin.Tests.Domain;

public class PlanQuotaTests
{
    private static readonly PlanQuota PlanQuota = new PlanQuota(1, 1, 1000, false, new Money((decimal)2.5, "EUR"));
    
    [Fact]
    public void PlanQuota_AllFieldsAreValid_FieldsAreSet()
    {
        // Assert
        PlanQuota.PlanId.Should().Be(1);
        PlanQuota.UsageMetricId.Should().Be(1);
        PlanQuota.Limit.Should().Be(1000);
        PlanQuota.HardLimit.Should().BeFalse(); 
        PlanQuota.OverageUnitPrice.Amount.Should().Be(2.5m);
        PlanQuota.OverageUnitPrice.Currency.Should().Be("EUR");
    }

    [Fact]
    public void IsUnlimited_LimitSet_UnlimitedFalse()
    {
        // Act
        var result = PlanQuota.IsUnlimited;

        // Assert
        result.Should().BeFalse();
    }
    
    [Fact]
    public void IsUnlimited_LimitNegative_UnlimitedTrue()
    {
        // Arrange
        var planQuota = new PlanQuota(1, 1, -1, false, new Money((decimal)2.5, "EUR"));
        
        // Act
        var result = planQuota.IsUnlimited;
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Fact]
    public void CalculateOverage_LimitNegative_ShouldBeOne()
    {
        // Arrange
        var planQuota = new PlanQuota(1, 1, -1, false, new Money((decimal)2.5, "EUR"));
        
        // Act
        var result = planQuota.CalculateOverage(10);
        
        // Assert
        result.Should().Be(0);
    }
    
    [Fact]
    public void CalculateOverage_LimitSetNotOver_Zero()
    {
        // Act
        var result = PlanQuota.CalculateOverage(10);
        
        // Assert
        result.Should().Be(0);
    }
    
    [Fact]
    public void CalculateOverage_LimitSetUsedNotOver_Zero()
    {
        // Act
        var result = PlanQuota.CalculateOverage(1009);
        
        // Assert
        result.Should().Be(9m);
    }
}
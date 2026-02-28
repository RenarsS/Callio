using Callio.Admin.Domain.Enums;
using Callio.Admin.Domain.Exceptions.Tenant;
using Callio.Admin.Domain.ValueObjects;
using FluentAssertions;

namespace Callio.Admin.Tests.Domain.ValueObjects;

public class TenancyStatusTests
{
    [Fact]
    public void TenancyStatus_NotActivated_ValueIsPending()
    {
        // Arrange
        var activatedAt = new DateTime(2026, 2, 28);
        var today = new DateTime(2026, 2, 27);
        
        // Act
        var tenancyStatus = new TenancyStatus(today, activatedAt);

        // Assert
        tenancyStatus.Value.Should().Be(Status.Pending);
    }
    
    [Fact]
    public void TenancyStatus_Activated_ValueIsEnabled()
    {
        // Arrange
        var activatedAt = new DateTime(2026, 2, 28);
        var today = new DateTime(2026, 2, 28);
        
        // Act
        var tenancyStatus = new TenancyStatus(today, activatedAt);

        // Assert
        tenancyStatus.Value.Should().Be(Status.Enabled);
    }
    
    [Fact]
    public void TenancyStatus_NotActivated_ValueIsDisabled()
    {
        // Arrange
        var activatedAt = new DateTime(2026, 2, 28);
        var deactivatedAt = new DateTime(2026, 3, 1);
        var today = new DateTime(2026, 3, 1);
        
        // Act
        var tenancyStatus = new TenancyStatus(today, activatedAt, deactivatedAt);

        // Assert
        tenancyStatus.Value.Should().Be(Status.Disabled);
    }
    
    [Fact]
    public void TenancyStatus_DeactivateBeforeActivated_ExceptionsIsThrown()
    {
        // Arrange
        var activatedAt = new DateTime(2026, 2, 28);
        var deactivatedAt = new DateTime(2026, 2, 27);
        var today = new DateTime(2026, 3, 1);
        
        // Act
        var act = () =>new TenancyStatus(today, activatedAt, deactivatedAt);

        // Assert
        act.Should().Throw<InvalidDateException>().WithMessage("Deactivation date must be after activation date.");
    }
}
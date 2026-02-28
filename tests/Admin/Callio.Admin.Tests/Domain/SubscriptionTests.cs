using Callio.Admin.Domain;
using Callio.Admin.Domain.Enums;
using Callio.Admin.Domain.ValueObjects;
using FluentAssertions;

namespace Callio.Admin.Tests.Domain;

public class SubscriptionTests
{
    [Fact]
    public void Subscription_AllFieldsAreValid_FieldsAreSet()
    {
        // Arrange
        var currentPeriod = new DateRange(new(2026, 1, 1), new(2026, 3, 1), new(2026, 2, 2));

        // Act
        var subscription = new Subscription(1, 1, currentPeriod);

        // Assert
        subscription.TenantId.Should().Be(1);
        subscription.PlanId.Should().Be(1);
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.CurrentPeriod.Should().Be(currentPeriod);
        subscription.TrialEndsAt.Should().BeNull();
        subscription.CancelledAt.Should().BeNull();
        subscription.CancellationScheduledAt.Should().BeNull();
    }
    
    [Fact]
    public void Subscription_AllFieldsAreValidTrial_FieldsAreSet()
    {
        // Arrange
        var currentPeriod = new DateRange(new(2026, 1, 1), new(2026, 3, 1), new(2026, 2, 2));
        var trialEndDate = new DateTime(2026, 2, 23);
        
        // Act
        var subscription = new Subscription(1, 1, currentPeriod, trialEndDate);

        // Assert
        subscription.TenantId.Should().Be(1);
        subscription.PlanId.Should().Be(1);
        subscription.Status.Should().Be(SubscriptionStatus.Trial);
        subscription.CurrentPeriod.Should().Be(currentPeriod);
        subscription.TrialEndsAt.Should().Be(trialEndDate);
        subscription.CancelledAt.Should().BeNull();
        subscription.CancellationScheduledAt.Should().BeNull();
    }
    
    [Fact]
    public void Activate_Trial_StatusSetActive()
    {
        // Arrange
        var currentPeriod = new DateRange(new(2026, 1, 1), new(2026, 3, 1), new(2026, 2, 2));
        var trialEndDate = new DateTime(2026, 2, 23);
        var subscription = new Subscription(1, 1, currentPeriod, trialEndDate);
        
        // Act
        subscription.Activate();

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.TrialEndsAt.Should().BeNull();
    }
    
    [Fact]
    public void Cancel_AtPeriodEnd_StatusSetCancel()
    {
        // Arrange
        var currentPeriod = new DateRange(new(2026, 1, 1), new(2026, 3, 1), new(2026, 2, 2));
        var subscription = new Subscription(1, 1, currentPeriod);
        
        // Act
        subscription.Cancel(false);

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.CancellationScheduledAt.Should().Be(new(2026, 3, 1));
    }
    
    [Fact]
    public void Cancel_Immediately_StatusSetCancel()
    {
        // Arrange
        var currentPeriod = new DateRange(new(2026, 1, 1), new(2026, 3, 1), new(2026, 2, 2));
        var subscription = new Subscription(1, 1, currentPeriod);
        
        // Act
        subscription.Cancel(true, new(2026, 2, 2));

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
        subscription.CancelledAt.Should().Be(new(2026, 2, 2));
    }
    
    [Fact]
    public void Renew_NewPeriodInFuture_StatusSetActive()
    {
        // Arrange
        var currentPeriod = new DateRange(new(2026, 1, 1), new(2026, 3, 1), new(2026, 2, 2));
        var subscription = new Subscription(1, 1, currentPeriod);
        subscription.Cancel(true, new(2026, 2, 2));

        var newPeriod = new DateRange(new(2026, 4, 2), new(2026, 4, 30), new(2026, 2, 2));
        
        // Act
        subscription.Renew(newPeriod);

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Pending);
    }
    
    [Fact]
    public void Renew_NewPeriodNow_StatusSetActive()
    {
        // Arrange
        var currentPeriod = new DateRange(new(2026, 1, 1), new(2026, 3, 1), new(2026, 2, 2));
        var subscription = new Subscription(1, 1, currentPeriod);
        subscription.Cancel(true, new(2026, 2, 2));

        var newPeriod = new DateRange(new(2026, 3, 2), new(2026, 5, 1), new(2026, 3, 2));
        
        // Act
        subscription.Renew(newPeriod);

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Active);
    }
    
    [Fact]
    public void MarkPastDue_PastDue_StatusSetPastDue()
    {
        // Arrange
        var currentPeriod = new DateRange(new(2026, 1, 1), new(2026, 3, 1), new(2026, 2, 2));
        var subscription = new Subscription(1, 1, currentPeriod);
        // Act
        subscription.MarkPastDue();

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.PastDue);
    }
    
    [Fact]
    public void Suspend_Suspended_StatusSetSuspended()
    {
        // Arrange
        var currentPeriod = new DateRange(new(2026, 1, 1), new(2026, 3, 1), new(2026, 2, 2));
        var subscription = new Subscription(1, 1, currentPeriod);
        
        // Act
        subscription.Suspend();

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Suspended);
    }
}
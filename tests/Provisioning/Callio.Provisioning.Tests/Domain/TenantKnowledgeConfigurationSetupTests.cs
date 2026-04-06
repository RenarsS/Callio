using Callio.Provisioning.Domain;
using Callio.Provisioning.Domain.Enums;
using FluentAssertions;

namespace Callio.Provisioning.Tests.Domain;

public class TenantKnowledgeConfigurationSetupTests
{
    [Fact]
    public void CreatePending_InitializesPendingStatus()
    {
        var now = new DateTime(2026, 4, 6, 14, 0, 0, DateTimeKind.Utc);

        var setup = TenantKnowledgeConfigurationSetup.CreatePending(42, now);

        setup.TenantId.Should().Be(42);
        setup.Status.Should().Be(KnowledgeConfigurationSetupStatus.Pending);
        setup.AttemptCount.Should().Be(0);
        setup.CreatedAtUtc.Should().Be(now);
        setup.UpdatedAtUtc.Should().Be(now);
    }

    [Fact]
    public void BeginAttempt_MarksInProgressAndClearsFailure()
    {
        var setup = TenantKnowledgeConfigurationSetup.CreatePending(42, DateTime.UtcNow);

        setup.MarkFailed("Failed once.", DateTime.UtcNow);
        var startedAt = new DateTime(2026, 4, 6, 14, 5, 0, DateTimeKind.Utc);

        setup.BeginAttempt(startedAt);

        setup.Status.Should().Be(KnowledgeConfigurationSetupStatus.InProgress);
        setup.AttemptCount.Should().Be(1);
        setup.LastError.Should().BeNull();
        setup.LastStartedAtUtc.Should().Be(startedAt);
    }

    [Fact]
    public void MarkSucceeded_CapturesConfigurationId()
    {
        var setup = TenantKnowledgeConfigurationSetup.CreatePending(42, DateTime.UtcNow);
        setup.BeginAttempt(DateTime.UtcNow);

        var completedAt = new DateTime(2026, 4, 6, 14, 10, 0, DateTimeKind.Utc);
        setup.MarkSucceeded(7, completedAt);

        setup.Status.Should().Be(KnowledgeConfigurationSetupStatus.Succeeded);
        setup.ActiveConfigurationId.Should().Be(7);
        setup.LastError.Should().BeNull();
        setup.LastCompletedAtUtc.Should().Be(completedAt);
    }
}

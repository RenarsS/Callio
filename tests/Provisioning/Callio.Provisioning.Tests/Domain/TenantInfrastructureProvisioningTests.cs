using Callio.Provisioning.Domain;
using Callio.Provisioning.Domain.Enums;
using FluentAssertions;

namespace Callio.Provisioning.Tests.Domain;

public class TenantInfrastructureProvisioningTests
{
    [Fact]
    public void Create_InitializesOrderedPendingStepsAndResourceNames()
    {
        var now = new DateTime(2026, 4, 6, 10, 30, 0, DateTimeKind.Utc);

        var provisioning = TenantInfrastructureProvisioning.Create(
            "user-123",
            42,
            1001,
            "tenant_42",
            "tenant-42",
            now);

        provisioning.Status.Should().Be(ProvisioningStatus.Pending);
        provisioning.DatabaseSchema.Should().Be("tenant_42");
        provisioning.VectorStoreNamespace.Should().Be("tenant-42");
        provisioning.Steps.Should().HaveCount(2);
        provisioning.Steps.Select(x => x.Name).Should().ContainInOrder(TenantProvisioningSteps.Ordered);
        provisioning.Steps.Should().OnlyContain(x => x.Status == ProvisioningStepStatus.Pending);
    }

    [Fact]
    public void BeginAttempt_IncrementsAttemptCountAndMarksInProgress()
    {
        var provisioning = TenantInfrastructureProvisioning.Create(
            "user-123",
            42,
            1001,
            "tenant_42",
            "tenant-42",
            DateTime.UtcNow);

        var startedAt = new DateTime(2026, 4, 6, 11, 0, 0, DateTimeKind.Utc);
        provisioning.BeginAttempt(startedAt);

        provisioning.Status.Should().Be(ProvisioningStatus.InProgress);
        provisioning.AttemptCount.Should().Be(1);
        provisioning.LastStartedAtUtc.Should().Be(startedAt);
        provisioning.LastError.Should().BeNull();
        provisioning.FailedStep.Should().BeNull();
    }

    [Fact]
    public void MarkFailed_CapturesFailedStepAndError()
    {
        var provisioning = TenantInfrastructureProvisioning.Create(
            "user-123",
            42,
            1001,
            "tenant_42",
            "tenant-42",
            DateTime.UtcNow);

        provisioning.BeginAttempt(DateTime.UtcNow);
        var failedAt = new DateTime(2026, 4, 6, 11, 30, 0, DateTimeKind.Utc);

        provisioning.MarkFailed(TenantProvisioningSteps.VectorStore, "Vector namespace could not be created.", failedAt);

        provisioning.Status.Should().Be(ProvisioningStatus.Failed);
        provisioning.FailedStep.Should().Be(TenantProvisioningSteps.VectorStore);
        provisioning.LastError.Should().Be("Vector namespace could not be created.");
        provisioning.LastCompletedAtUtc.Should().Be(failedAt);
    }

    [Fact]
    public void MarkSucceeded_ClearsFailureState()
    {
        var provisioning = TenantInfrastructureProvisioning.Create(
            "user-123",
            42,
            1001,
            "tenant_42",
            "tenant-42",
            DateTime.UtcNow);

        provisioning.BeginAttempt(DateTime.UtcNow);
        provisioning.MarkFailed(TenantProvisioningSteps.VectorStore, "Vector setup failed.", DateTime.UtcNow);

        var completedAt = new DateTime(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc);
        provisioning.MarkSucceeded(completedAt);

        provisioning.Status.Should().Be(ProvisioningStatus.Succeeded);
        provisioning.FailedStep.Should().BeNull();
        provisioning.LastError.Should().BeNull();
        provisioning.LastCompletedAtUtc.Should().Be(completedAt);
    }
}

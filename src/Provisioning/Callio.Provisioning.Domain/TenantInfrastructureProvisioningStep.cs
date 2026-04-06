using Callio.Core.Domain.Exceptions;
using Callio.Core.Domain.Helpers;
using Callio.Provisioning.Domain.Enums;

namespace Callio.Provisioning.Domain;

public class TenantInfrastructureProvisioningStep : Entity<int>
{
    public int TenantInfrastructureProvisioningId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int Order { get; private set; }

    public ProvisioningStepStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public string? LastError { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? LastStartedAtUtc { get; private set; }

    public DateTime? LastCompletedAtUtc { get; private set; }

    private TenantInfrastructureProvisioningStep()
    {
    }

    internal TenantInfrastructureProvisioningStep(string name, int order, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidFieldException(nameof(Name));

        Name = name.Trim();
        Order = order;
        Status = ProvisioningStepStatus.Pending;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void Reset(DateTime now)
    {
        Status = ProvisioningStepStatus.Pending;
        LastError = null;
        LastStartedAtUtc = null;
        LastCompletedAtUtc = null;
        UpdatedAtUtc = now;
    }

    public void MarkInProgress(DateTime now)
    {
        Status = ProvisioningStepStatus.InProgress;
        AttemptCount++;
        LastError = null;
        LastStartedAtUtc = now;
        LastCompletedAtUtc = null;
        UpdatedAtUtc = now;
    }

    public void MarkSucceeded(DateTime now)
    {
        Status = ProvisioningStepStatus.Succeeded;
        LastError = null;
        LastCompletedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void MarkFailed(string errorMessage, DateTime now)
    {
        Status = ProvisioningStepStatus.Failed;
        LastError = string.IsNullOrWhiteSpace(errorMessage) ? "Provisioning step failed." : errorMessage.Trim();
        LastCompletedAtUtc = now;
        UpdatedAtUtc = now;
    }
}

using Callio.Core.Domain.Exceptions;
using Callio.Core.Domain.Helpers;
using Callio.Provisioning.Domain.Enums;

namespace Callio.Provisioning.Domain;

public class TenantInfrastructureProvisioning : Entity<int>
{
    public int TenantId { get; private set; }

    public int TenantRequestId { get; private set; }

    public string RequestedByUserId { get; private set; } = string.Empty;

    public ProvisioningStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public string DatabaseSchema { get; private set; } = string.Empty;

    public string VectorStoreNamespace { get; private set; } = string.Empty;

    public string? FailedStep { get; private set; }

    public string? LastError { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? LastStartedAtUtc { get; private set; }

    public DateTime? LastCompletedAtUtc { get; private set; }

    public ICollection<TenantInfrastructureProvisioningStep> Steps { get; private set; } = [];

    private TenantInfrastructureProvisioning()
    {
    }

    private TenantInfrastructureProvisioning(
        string requestedByUserId,
        int tenantId,
        int tenantRequestId,
        string databaseSchema,
        string vectorStoreNamespace,
        DateTime now)
    {
        if (string.IsNullOrWhiteSpace(requestedByUserId))
            throw new InvalidFieldException(nameof(RequestedByUserId));

        if (string.IsNullOrWhiteSpace(databaseSchema))
            throw new InvalidFieldException(nameof(DatabaseSchema));

        if (string.IsNullOrWhiteSpace(vectorStoreNamespace))
            throw new InvalidFieldException(nameof(VectorStoreNamespace));

        RequestedByUserId = requestedByUserId.Trim();
        TenantId = tenantId;
        TenantRequestId = tenantRequestId;
        DatabaseSchema = databaseSchema.Trim();
        VectorStoreNamespace = vectorStoreNamespace.Trim();
        Status = ProvisioningStatus.Pending;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;

        for (var i = 0; i < TenantProvisioningSteps.Ordered.Count; i++)
        {
            Steps.Add(new TenantInfrastructureProvisioningStep(TenantProvisioningSteps.Ordered[i], i + 1, now));
        }
    }

    public static TenantInfrastructureProvisioning Create(
        string requestedByUserId,
        int tenantId,
        int tenantRequestId,
        string databaseSchema,
        string vectorStoreNamespace,
        DateTime now)
        => new(requestedByUserId, tenantId, tenantRequestId, databaseSchema, vectorStoreNamespace, now);

    public void RefreshSource(string requestedByUserId, int tenantRequestId, DateTime now)
    {
        if (!string.IsNullOrWhiteSpace(requestedByUserId))
            RequestedByUserId = requestedByUserId.Trim();

        TenantRequestId = tenantRequestId;
        UpdatedAtUtc = now;
    }

    public void BeginAttempt(DateTime now)
    {
        AttemptCount++;
        Status = ProvisioningStatus.InProgress;
        FailedStep = null;
        LastError = null;
        LastStartedAtUtc = now;
        LastCompletedAtUtc = null;
        UpdatedAtUtc = now;
    }

    public void MarkFailed(string failedStep, string errorMessage, DateTime now)
    {
        Status = ProvisioningStatus.Failed;
        FailedStep = string.IsNullOrWhiteSpace(failedStep) ? null : failedStep.Trim();
        LastError = string.IsNullOrWhiteSpace(errorMessage) ? "Tenant infrastructure provisioning failed." : errorMessage.Trim();
        LastCompletedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void MarkSucceeded(DateTime now)
    {
        Status = ProvisioningStatus.Succeeded;
        FailedStep = null;
        LastError = null;
        LastCompletedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public TenantInfrastructureProvisioningStep GetRequiredStep(string name)
    {
        var step = Steps.FirstOrDefault(x => x.Name == name);
        if (step is null)
            throw new InvalidOperationException($"Provisioning step '{name}' was not found.");

        return step;
    }
}

using Callio.Core.Domain.Helpers;
using Callio.Knowledge.Domain.Enums;

namespace Callio.Knowledge.Domain;

public class TenantKnowledgeConfigurationSetup : Entity<int>
{
    public int TenantId { get; private set; }

    public KnowledgeConfigurationSetupStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public int? ActiveConfigurationId { get; private set; }

    public string? LastError { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? LastStartedAtUtc { get; private set; }

    public DateTime? LastCompletedAtUtc { get; private set; }

    private TenantKnowledgeConfigurationSetup()
    {
    }

    private TenantKnowledgeConfigurationSetup(int tenantId, DateTime now)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantId), "Tenant id must be greater than zero.");

        TenantId = tenantId;
        Status = KnowledgeConfigurationSetupStatus.Pending;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public static TenantKnowledgeConfigurationSetup CreatePending(int tenantId, DateTime now)
        => new(tenantId, now);

    public void BeginAttempt(DateTime now)
    {
        AttemptCount++;
        Status = KnowledgeConfigurationSetupStatus.InProgress;
        LastError = null;
        LastStartedAtUtc = now;
        LastCompletedAtUtc = null;
        UpdatedAtUtc = now;
    }

    public void MarkSucceeded(int activeConfigurationId, DateTime now)
    {
        if (activeConfigurationId <= 0)
            throw new ArgumentOutOfRangeException(nameof(activeConfigurationId), "Configuration id must be greater than zero.");

        Status = KnowledgeConfigurationSetupStatus.Succeeded;
        ActiveConfigurationId = activeConfigurationId;
        LastError = null;
        LastCompletedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void MarkFailed(string? errorMessage, DateTime now)
    {
        Status = KnowledgeConfigurationSetupStatus.Failed;
        LastError = string.IsNullOrWhiteSpace(errorMessage)
            ? "Tenant knowledge configuration setup failed."
            : errorMessage.Trim();
        LastCompletedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void RefreshPending(DateTime now)
    {
        if (Status == KnowledgeConfigurationSetupStatus.Succeeded)
            return;

        Status = KnowledgeConfigurationSetupStatus.Pending;
        UpdatedAtUtc = now;
    }
}

using Callio.Core.Domain.Helpers;

namespace Callio.Admin.Domain;

public class UsageRecord(
    int tenantId,
    int usageMetricId,
    decimal quantity,
    DateTime? recordedAt = null,
    string? sourceReference = null)
    : Entity<int>
{
    public int TenantId { get; private set; } = tenantId;

    public int UsageMetricId { get; private set; } = usageMetricId;

    public decimal Quantity { get; private set; } = quantity;

    public DateTime RecordedAt { get; private set; } = recordedAt ?? DateTime.Now;

    public string? SourceReference { get; private set; } = sourceReference; // request ID, job ID, etc.
}
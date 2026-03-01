using Callio.Core.Domain.Helpers;

namespace Callio.Admin.Domain;

public class UsageRecord : Entity<int>
{
    public int TenantId { get; private set; }

    public int UsageMetricId { get; private set; }

    public decimal Quantity { get; private set; }

    public DateTime RecordedAt { get; private set; }

    public string? SourceReference { get; private set; }

    private UsageRecord() { }
    
    public UsageRecord(
        int tenantId,
        int usageMetricId,
        decimal quantity,
        DateTime? recordedAt = null,
        string? sourceReference = null)
    {
        TenantId = tenantId;
        UsageMetricId = usageMetricId;
        Quantity = quantity;
        RecordedAt = recordedAt ?? DateTime.Now;
        SourceReference = sourceReference;
    }
}
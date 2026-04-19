using Callio.Core.Domain.Helpers;

namespace Callio.Provisioning.Domain;

public class TenantKnowledgeDocumentTag : Entity<int>
{
    public int TenantKnowledgeDocumentId { get; private set; }

    public TenantKnowledgeDocument Document { get; private set; } = null!;

    public int TenantKnowledgeTagId { get; private set; }

    public TenantKnowledgeTag Tag { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    private TenantKnowledgeDocumentTag()
    {
    }

    public TenantKnowledgeDocumentTag(int tenantKnowledgeTagId, DateTime now)
    {
        if (tenantKnowledgeTagId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantKnowledgeTagId), "Tag id must be greater than zero.");

        TenantKnowledgeTagId = tenantKnowledgeTagId;
        CreatedAtUtc = now;
    }
}

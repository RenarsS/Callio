using Callio.Core.Domain.Exceptions;
using Callio.Core.Domain.Helpers;

namespace Callio.Provisioning.Domain;

public class TenantKnowledgeTag : Entity<int>
{
    private const int MaxNameLength = 80;

    public int TenantId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public ICollection<TenantKnowledgeDocumentTag> DocumentTags { get; private set; } = [];

    private TenantKnowledgeTag()
    {
    }

    public TenantKnowledgeTag(int tenantId, string name, DateTime now)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantId), "Tenant id must be greater than zero.");

        TenantId = tenantId;
        Name = NormalizeName(name);
        NormalizedName = Name.ToUpperInvariant();
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void Rename(string name, DateTime now)
    {
        Name = NormalizeName(name);
        NormalizedName = Name.ToUpperInvariant();
        UpdatedAtUtc = now;
    }

    private static string NormalizeName(string value)
    {
        var normalized = string.Join(' ', (value ?? string.Empty)
            .Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidFieldException(nameof(Name));

        if (normalized.Length > MaxNameLength)
            throw new ArgumentOutOfRangeException(nameof(Name), $"Tag name cannot exceed {MaxNameLength} characters.");

        return normalized;
    }
}

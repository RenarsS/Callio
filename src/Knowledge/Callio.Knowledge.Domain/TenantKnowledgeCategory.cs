using Callio.Core.Domain.Exceptions;
using Callio.Core.Domain.Helpers;

namespace Callio.Knowledge.Domain;

public class TenantKnowledgeCategory : Entity<int>
{
    private const int MaxNameLength = 120;
    private const int MaxDescriptionLength = 500;

    public int TenantId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public ICollection<TenantKnowledgeDocument> Documents { get; private set; } = [];

    private TenantKnowledgeCategory()
    {
    }

    public TenantKnowledgeCategory(int tenantId, string name, string? description, DateTime now)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantId), "Tenant id must be greater than zero.");

        TenantId = tenantId;
        Name = NormalizeName(name);
        NormalizedName = Name.ToUpperInvariant();
        Description = NormalizeOptional(description, MaxDescriptionLength, nameof(description));
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void Update(string name, string? description, DateTime now)
    {
        Name = NormalizeName(name);
        NormalizedName = Name.ToUpperInvariant();
        Description = NormalizeOptional(description, MaxDescriptionLength, nameof(description));
        UpdatedAtUtc = now;
    }

    private static string NormalizeName(string value)
    {
        var normalized = NormalizeWhitespace(value);
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidFieldException(nameof(Name));

        if (normalized.Length > MaxNameLength)
            throw new ArgumentOutOfRangeException(nameof(Name), $"Category name cannot exceed {MaxNameLength} characters.");

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string fieldName)
    {
        var normalized = NormalizeWhitespace(value);
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        if (normalized.Length > maxLength)
            throw new ArgumentOutOfRangeException(fieldName, $"{fieldName} cannot exceed {maxLength} characters.");

        return normalized;
    }

    private static string NormalizeWhitespace(string? value)
        => string.Join(' ', (value ?? string.Empty)
            .Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}

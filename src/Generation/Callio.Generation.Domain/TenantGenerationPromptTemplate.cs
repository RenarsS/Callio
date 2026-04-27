using Callio.Core.Domain.Exceptions;
using Callio.Core.Domain.Helpers;

namespace Callio.Generation.Domain;

public class TenantGenerationPromptTemplate : Entity<int>
{
    private const int MaxPromptKeyLength = 120;
    private const int MaxPromptNameLength = 200;
    private const int MaxDescriptionLength = 1000;

    public int TenantId { get; private set; }

    public string Key { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string SystemPrompt { get; private set; } = string.Empty;

    public string UserPromptTemplate { get; private set; } = string.Empty;

    public string DataSourcesJson { get; private set; } = "[]";

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    private TenantGenerationPromptTemplate()
    {
    }

    public TenantGenerationPromptTemplate(
        int tenantId,
        string key,
        string name,
        string? description,
        string systemPrompt,
        string userPromptTemplate,
        string dataSourcesJson,
        DateTime now)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantId), "Tenant id must be greater than zero.");

        TenantId = tenantId;
        Key = NormalizeRequired(key, MaxPromptKeyLength, nameof(Key));
        Name = NormalizeRequired(name, MaxPromptNameLength, nameof(Name));
        Description = NormalizeOptional(description, MaxDescriptionLength, nameof(Description));
        SystemPrompt = NormalizeRequired(systemPrompt, int.MaxValue, nameof(SystemPrompt));
        UserPromptTemplate = NormalizeRequired(userPromptTemplate, int.MaxValue, nameof(UserPromptTemplate));
        DataSourcesJson = NormalizeJson(dataSourcesJson);
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void Update(
        string key,
        string name,
        string? description,
        string systemPrompt,
        string userPromptTemplate,
        string dataSourcesJson,
        DateTime now)
    {
        Key = NormalizeRequired(key, MaxPromptKeyLength, nameof(Key));
        Name = NormalizeRequired(name, MaxPromptNameLength, nameof(Name));
        Description = NormalizeOptional(description, MaxDescriptionLength, nameof(Description));
        SystemPrompt = NormalizeRequired(systemPrompt, int.MaxValue, nameof(SystemPrompt));
        UserPromptTemplate = NormalizeRequired(userPromptTemplate, int.MaxValue, nameof(UserPromptTemplate));
        DataSourcesJson = NormalizeJson(dataSourcesJson);
        UpdatedAtUtc = now;
    }

    private static string NormalizeRequired(string? value, int maxLength, string fieldName)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
            throw new InvalidFieldException(fieldName);

        if (normalized.Length > maxLength)
            throw new ArgumentOutOfRangeException(fieldName, $"{fieldName} cannot exceed {maxLength} characters.");

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string fieldName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        if (normalized.Length > maxLength)
            throw new ArgumentOutOfRangeException(fieldName, $"{fieldName} cannot exceed {maxLength} characters.");

        return normalized;
    }

    private static string NormalizeJson(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "[]" : normalized;
    }
}

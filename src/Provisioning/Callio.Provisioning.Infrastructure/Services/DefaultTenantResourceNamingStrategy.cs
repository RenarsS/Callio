using Callio.Provisioning.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Callio.Provisioning.Infrastructure.Services;

public class DefaultTenantResourceNamingStrategy(IOptions<TenantProvisioningOptions> options) : ITenantResourceNamingStrategy
{
    private readonly TenantProvisioningOptions _options = options.Value;

    public TenantProvisioningResourceNames Create(int tenantId)
    {
        var schemaPrefix = EnsureSuffix(SanitizeDatabasePrefix(_options.SchemaPrefix), '_');
        var vectorPrefix = EnsureSuffix(SanitizeNamespacePrefix(_options.VectorNamespacePrefix), '-');
        var blobPrefix = EnsureSuffix(SanitizeBlobContainerPrefix(_options.BlobContainerPrefix), '-');

        return new TenantProvisioningResourceNames(
            $"{schemaPrefix}{tenantId}",
            $"{vectorPrefix}{tenantId}",
            NormalizeBlobContainerName($"{blobPrefix}{tenantId}"));
    }

    private static string SanitizeDatabasePrefix(string? value)
    {
        var sanitized = new string((value ?? "tenant_")
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "tenant_" : sanitized;
    }

    private static string SanitizeNamespacePrefix(string? value)
    {
        var sanitized = new string((value ?? "tenant-")
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '-')
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "tenant-" : sanitized;
    }

    private static string SanitizeBlobContainerPrefix(string? value)
    {
        var sanitized = new string((value ?? "tenant-knowledge-")
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) || ch == '-' ? ch : '-')
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "tenant-knowledge-" : sanitized;
    }

    private static string NormalizeBlobContainerName(string value)
    {
        var normalized = string.Join(
            '-',
            value
                .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => x.Length > 0));

        normalized = normalized.Length switch
        {
            < 3 => normalized.PadRight(3, '0'),
            > 63 => normalized[..63].TrimEnd('-'),
            _ => normalized
        };

        return string.IsNullOrWhiteSpace(normalized) ? "tenant-knowledge" : normalized;
    }

    private static string EnsureSuffix(string value, char separator)
        => value.EndsWith(separator) ? value : $"{value}{separator}";
}

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

        return new TenantProvisioningResourceNames(
            $"{schemaPrefix}{tenantId}",
            $"{vectorPrefix}{tenantId}");
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

    private static string EnsureSuffix(string value, char separator)
        => value.EndsWith(separator) ? value : $"{value}{separator}";
}

using Callio.Knowledge.Infrastructure.Options;
using Callio.Provisioning.Infrastructure.Options;
using Callio.Provisioning.Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Callio.Knowledge.Infrastructure.Services.KnowledgeDocuments;

public class FileSystemTenantKnowledgeBlobStorage(
    IOptions<TenantProvisioningOptions> provisioningOptions,
    IOptions<TenantKnowledgeIngestionOptions> ingestionOptions,
    ITenantResourceNamingStrategy tenantResourceNamingStrategy,
    IHostEnvironment hostEnvironment) : ITenantKnowledgeBlobStorage
{
    private readonly TenantProvisioningOptions _provisioningOptions = provisioningOptions.Value;
    private readonly TenantKnowledgeIngestionOptions _ingestionOptions = ingestionOptions.Value;

    public async Task<TenantKnowledgeBlobObject> UploadAsync(
        int tenantId,
        string fileName,
        string contentType,
        byte[] content,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken = default)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantId));

        var rootPath = ResolveRootPath();

        var safeFileName = Path.GetFileName(fileName);
        var containerName = tenantResourceNamingStrategy.Create(tenantId).BlobContainerName;
        var blobName = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}/{safeFileName}";
        var fullPath = Path.Combine(rootPath, containerName, blobName.Replace('/', Path.DirectorySeparatorChar));
        var directory = Path.GetDirectoryName(fullPath)
                        ?? throw new InvalidOperationException("Blob storage directory could not be resolved.");

        Directory.CreateDirectory(directory);
        await File.WriteAllBytesAsync(fullPath, content, cancellationToken);

        return new TenantKnowledgeBlobObject(containerName, blobName, fullPath);
    }

    public async Task<TenantKnowledgeBlobContent> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name is required.", nameof(blobName));

        var rootPath = ResolveRootPath();
        var resolvedContainerName = string.IsNullOrWhiteSpace(containerName) ? "local-tenant-knowledge" : containerName.Trim();
        var fullPath = Path.GetFullPath(Path.Combine(rootPath, resolvedContainerName, blobName.Replace('/', Path.DirectorySeparatorChar)));

        if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Blob path resolved outside the configured storage root.");

        if (!File.Exists(fullPath))
            fullPath = Path.GetFullPath(Path.Combine(rootPath, blobName.Replace('/', Path.DirectorySeparatorChar)));

        if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Blob path resolved outside the configured storage root.");

        var content = await File.ReadAllBytesAsync(fullPath, cancellationToken);
        return new TenantKnowledgeBlobContent(
            resolvedContainerName,
            blobName,
            "application/octet-stream",
            content);
    }

    private string ResolveRootPath()
    {
        var rootPath = string.IsNullOrWhiteSpace(_provisioningOptions.LocalBlobStorageRootPath)
            ? _ingestionOptions.LocalStorageRootPath
            : _provisioningOptions.LocalBlobStorageRootPath;

        if (!Path.IsPathRooted(rootPath))
            rootPath = Path.Combine(hostEnvironment.ContentRootPath, rootPath);

        return Path.GetFullPath(rootPath);
    }
}

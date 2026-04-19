using Callio.Provisioning.Infrastructure.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Callio.Provisioning.Infrastructure.Services.KnowledgeDocuments;

public class FileSystemTenantKnowledgeBlobStorage(
    IOptions<TenantKnowledgeIngestionOptions> options,
    IHostEnvironment hostEnvironment) : ITenantKnowledgeBlobStorage
{
    private readonly TenantKnowledgeIngestionOptions _options = options.Value;

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

        var rootPath = _options.LocalStorageRootPath;
        if (!Path.IsPathRooted(rootPath))
            rootPath = Path.Combine(hostEnvironment.ContentRootPath, rootPath);

        var safeFileName = Path.GetFileName(fileName);
        var blobName = $"{tenantId}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}/{safeFileName}";
        var fullPath = Path.Combine(rootPath, blobName.Replace('/', Path.DirectorySeparatorChar));
        var directory = Path.GetDirectoryName(fullPath)
                        ?? throw new InvalidOperationException("Blob storage directory could not be resolved.");

        Directory.CreateDirectory(directory);
        await File.WriteAllBytesAsync(fullPath, content, cancellationToken);

        return new TenantKnowledgeBlobObject("local-tenant-knowledge", blobName, fullPath);
    }
}

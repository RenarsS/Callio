using Callio.Provisioning.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Callio.Provisioning.Infrastructure.Services.KnowledgeDocuments;

public class TenantKnowledgeBlobStorage(
    IOptions<TenantKnowledgeIngestionOptions> options,
    FileSystemTenantKnowledgeBlobStorage fileSystemStorage,
    AzureBlobTenantKnowledgeBlobStorage azureBlobStorage) : ITenantKnowledgeBlobStorage
{
    private readonly TenantKnowledgeIngestionOptions _options = options.Value;

    public Task<TenantKnowledgeBlobObject> UploadAsync(
        int tenantId,
        string fileName,
        string contentType,
        byte[] content,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken = default)
        => IsAzureBlob()
            ? azureBlobStorage.UploadAsync(tenantId, fileName, contentType, content, metadata, cancellationToken)
            : fileSystemStorage.UploadAsync(tenantId, fileName, contentType, content, metadata, cancellationToken);

    private bool IsAzureBlob()
        => string.Equals(_options.BlobProvider, "AzureBlob", StringComparison.OrdinalIgnoreCase);
}

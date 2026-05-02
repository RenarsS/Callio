using Callio.Knowledge.Infrastructure.Options;
using Callio.Provisioning.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Callio.Knowledge.Infrastructure.Services.KnowledgeDocuments;

public class TenantKnowledgeBlobStorage(
    IOptions<TenantProvisioningOptions> provisioningOptions,
    IOptions<TenantKnowledgeIngestionOptions> ingestionOptions,
    FileSystemTenantKnowledgeBlobStorage fileSystemStorage,
    AzureBlobTenantKnowledgeBlobStorage azureBlobStorage) : ITenantKnowledgeBlobStorage
{
    private readonly TenantProvisioningOptions _provisioningOptions = provisioningOptions.Value;
    private readonly TenantKnowledgeIngestionOptions _ingestionOptions = ingestionOptions.Value;

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

    public Task<TenantKnowledgeBlobContent> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
        => IsAzureBlob()
            ? azureBlobStorage.DownloadAsync(containerName, blobName, cancellationToken)
            : fileSystemStorage.DownloadAsync(containerName, blobName, cancellationToken);

    private bool IsAzureBlob()
    {
        var provider = string.IsNullOrWhiteSpace(_provisioningOptions.BlobStorageProvider)
            && string.Equals(_provisioningOptions.ResourceProvider, TenantProvisioningOptions.LocalProvider, StringComparison.OrdinalIgnoreCase)
                ? _ingestionOptions.BlobProvider
                : _provisioningOptions.ResolveBlobStorageProvider();

        return string.Equals(provider, TenantProvisioningOptions.AzureBlobProvider, StringComparison.OrdinalIgnoreCase);
    }
}

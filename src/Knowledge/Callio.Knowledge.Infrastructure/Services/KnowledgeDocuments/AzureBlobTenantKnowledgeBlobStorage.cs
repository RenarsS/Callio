using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Callio.Knowledge.Infrastructure.Options;
using Callio.Provisioning.Infrastructure.Options;
using Callio.Provisioning.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace Callio.Knowledge.Infrastructure.Services.KnowledgeDocuments;

public class AzureBlobTenantKnowledgeBlobStorage(
    IOptions<TenantProvisioningOptions> provisioningOptions,
    IOptions<TenantKnowledgeIngestionOptions> ingestionOptions,
    ITenantResourceNamingStrategy tenantResourceNamingStrategy) : ITenantKnowledgeBlobStorage
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
        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Azure blob storage requires a connection string.");

        var blobName = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}/{Path.GetFileName(fileName)}";
        var containerClient = new BlobContainerClient(
            connectionString,
            tenantResourceNamingStrategy.Create(tenantId).BlobContainerName);

        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(blobName);
        await using var stream = new MemoryStream(content, writable: false);
        await blobClient.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
                },
                Metadata = metadata?.ToDictionary(x => x.Key, x => x.Value) ?? new Dictionary<string, string>()
            },
            cancellationToken);

        return new TenantKnowledgeBlobObject(containerClient.Name, blobName, blobClient.Uri.ToString());
    }

    public async Task<TenantKnowledgeBlobContent> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Azure blob storage requires a connection string.");

        var resolvedContainerName = string.IsNullOrWhiteSpace(containerName)
            ? _ingestionOptions.AzureBlobContainerName
            : containerName.Trim();

        if (string.IsNullOrWhiteSpace(resolvedContainerName))
            throw new InvalidOperationException("Azure blob storage requires a container name.");

        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name is required.", nameof(blobName));

        var containerClient = new BlobContainerClient(connectionString, resolvedContainerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        var download = await blobClient.DownloadContentAsync(cancellationToken);

        return new TenantKnowledgeBlobContent(
            resolvedContainerName,
            blobName,
            download.Value.Details.ContentType ?? "application/octet-stream",
            download.Value.Content.ToArray());
    }

    private string ResolveConnectionString()
        => string.IsNullOrWhiteSpace(_provisioningOptions.AzureBlobConnectionString)
            ? _ingestionOptions.AzureBlobConnectionString
            : _provisioningOptions.AzureBlobConnectionString;
}

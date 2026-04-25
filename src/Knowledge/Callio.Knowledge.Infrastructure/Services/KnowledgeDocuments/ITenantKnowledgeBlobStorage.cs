namespace Callio.Knowledge.Infrastructure.Services.KnowledgeDocuments;

public interface ITenantKnowledgeBlobStorage
{
    Task<TenantKnowledgeBlobObject> UploadAsync(
        int tenantId,
        string fileName,
        string contentType,
        byte[] content,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken = default);

    Task<TenantKnowledgeBlobContent> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);
}

namespace Callio.Provisioning.Infrastructure.Services.KnowledgeDocuments;

public interface ITenantKnowledgeBlobStorage
{
    Task<TenantKnowledgeBlobObject> UploadAsync(
        int tenantId,
        string fileName,
        string contentType,
        byte[] content,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken = default);
}

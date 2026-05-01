namespace Callio.Knowledge.Infrastructure.Services.KnowledgeDocuments;

public interface ITenantKnowledgeVectorStore
{
    bool UsesExternalVectorStore { get; }

    Task IndexChunksAsync(
        string vectorStoreNamespace,
        IReadOnlyList<TenantKnowledgeVectorRecord> records,
        CancellationToken cancellationToken = default);

    Task DeleteChunksAsync(
        string vectorStoreNamespace,
        string sectionKey,
        IReadOnlyList<string> vectorRecordIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantKnowledgeVectorSearchResult>> SearchAsync(
        string vectorStoreNamespace,
        TenantKnowledgeVectorSearchQuery query,
        CancellationToken cancellationToken = default);
}

public sealed record TenantKnowledgeVectorRecord(
    string Id,
    string VectorStoreNamespace,
    string DocumentKey,
    int ChunkIndex,
    string SectionKey,
    string SectionName,
    int? CategoryId,
    string? CategoryName,
    IReadOnlyList<int> TagIds,
    IReadOnlyList<string> TagNames,
    string DocumentTitle,
    string BlobContainerName,
    string BlobName,
    string BlobUri,
    string EmbeddingModel,
    string Content,
    float[] ContentVector);

public sealed record TenantKnowledgeVectorSearchQuery(
    float[] QueryVector,
    int Top,
    IReadOnlyList<string> SectionKeys,
    IReadOnlyList<string> DocumentKeys,
    IReadOnlyList<int> CategoryIds,
    IReadOnlyList<int> TagIds);

public sealed record TenantKnowledgeVectorSearchResult(
    string VectorRecordId,
    Guid DocumentKey,
    int ChunkIndex,
    decimal Score);

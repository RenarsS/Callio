using Callio.Core.Domain.Exceptions;
using Callio.Core.Domain.Helpers;

namespace Callio.Knowledge.Domain;

public class TenantKnowledgeDocumentChunk : Entity<int>
{
    private const int MaxVectorRecordIdLength = 256;
    private const int MaxVectorNamespaceLength = 256;
    private const int MaxEmbeddingModelLength = 256;

    public int TenantKnowledgeDocumentId { get; private set; }

    public TenantKnowledgeDocument Document { get; private set; } = null!;

    public int ChunkIndex { get; private set; }

    public string VectorRecordId { get; private set; } = string.Empty;

    public string VectorStoreNamespace { get; private set; } = string.Empty;

    public string EmbeddingModel { get; private set; } = string.Empty;

    public int EmbeddingDimensions { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public int CharacterCount { get; private set; }

    public int TokenCountEstimate { get; private set; }

    public string EmbeddingJson { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    private TenantKnowledgeDocumentChunk()
    {
    }

    public TenantKnowledgeDocumentChunk(
        int chunkIndex,
        string vectorRecordId,
        string vectorStoreNamespace,
        string embeddingModel,
        int embeddingDimensions,
        string content,
        string embeddingJson,
        DateTime now)
    {
        if (chunkIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(chunkIndex), "Chunk index cannot be negative.");

        if (embeddingDimensions <= 0)
            throw new ArgumentOutOfRangeException(nameof(embeddingDimensions), "Embedding dimensions must be greater than zero.");

        ChunkIndex = chunkIndex;
        VectorRecordId = NormalizeRequired(vectorRecordId, MaxVectorRecordIdLength, nameof(VectorRecordId));
        VectorStoreNamespace = NormalizeRequired(vectorStoreNamespace, MaxVectorNamespaceLength, nameof(VectorStoreNamespace));
        EmbeddingModel = NormalizeRequired(embeddingModel, MaxEmbeddingModelLength, nameof(EmbeddingModel));
        EmbeddingDimensions = embeddingDimensions;
        Content = NormalizeRequired(content, int.MaxValue, nameof(Content));
        CharacterCount = Content.Length;
        TokenCountEstimate = Math.Max(1, (int)Math.Ceiling(CharacterCount / 4d));
        EmbeddingJson = NormalizeRequired(embeddingJson, int.MaxValue, nameof(EmbeddingJson));
        CreatedAtUtc = now;
    }

    private static string NormalizeRequired(string value, int maxLength, string fieldName)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
            throw new InvalidFieldException(fieldName);

        if (normalized.Length > maxLength)
            throw new ArgumentOutOfRangeException(fieldName, $"{fieldName} cannot exceed {maxLength} characters.");

        return normalized;
    }
}

using Callio.Core.Domain.Exceptions;
using Callio.Core.Domain.Helpers;

namespace Callio.Provisioning.Domain;

public class TenantKnowledgeBaseSettings : Entity<int>
{
    public int TenantId { get; private set; }

    public string DatabaseSchema { get; private set; } = string.Empty;

    public string VectorStoreNamespace { get; private set; } = string.Empty;

    public string EmbeddingProvider { get; private set; } = string.Empty;

    public string EmbeddingModel { get; private set; } = string.Empty;

    public int ChunkSize { get; private set; }

    public int ChunkOverlap { get; private set; }

    public int RetrievalTopK { get; private set; }

    public bool IngestionEnabled { get; private set; }

    public bool RetrievalEnabled { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    private TenantKnowledgeBaseSettings()
    {
    }

    public TenantKnowledgeBaseSettings(
        int tenantId,
        string databaseSchema,
        string vectorStoreNamespace,
        string embeddingProvider,
        string embeddingModel,
        int chunkSize,
        int chunkOverlap,
        int retrievalTopK,
        bool ingestionEnabled,
        bool retrievalEnabled,
        DateTime now)
    {
        if (string.IsNullOrWhiteSpace(databaseSchema))
            throw new InvalidFieldException(nameof(DatabaseSchema));

        if (string.IsNullOrWhiteSpace(vectorStoreNamespace))
            throw new InvalidFieldException(nameof(VectorStoreNamespace));

        if (string.IsNullOrWhiteSpace(embeddingProvider))
            throw new InvalidFieldException(nameof(EmbeddingProvider));

        if (string.IsNullOrWhiteSpace(embeddingModel))
            throw new InvalidFieldException(nameof(EmbeddingModel));

        TenantId = tenantId;
        DatabaseSchema = databaseSchema.Trim();
        VectorStoreNamespace = vectorStoreNamespace.Trim();
        EmbeddingProvider = embeddingProvider.Trim();
        EmbeddingModel = embeddingModel.Trim();
        ChunkSize = chunkSize;
        ChunkOverlap = chunkOverlap;
        RetrievalTopK = retrievalTopK;
        IngestionEnabled = ingestionEnabled;
        RetrievalEnabled = retrievalEnabled;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void UpdateDefaults(
        string databaseSchema,
        string vectorStoreNamespace,
        string embeddingProvider,
        string embeddingModel,
        int chunkSize,
        int chunkOverlap,
        int retrievalTopK,
        bool ingestionEnabled,
        bool retrievalEnabled,
        DateTime now)
    {
        DatabaseSchema = databaseSchema.Trim();
        VectorStoreNamespace = vectorStoreNamespace.Trim();
        EmbeddingProvider = embeddingProvider.Trim();
        EmbeddingModel = embeddingModel.Trim();
        ChunkSize = chunkSize;
        ChunkOverlap = chunkOverlap;
        RetrievalTopK = retrievalTopK;
        IngestionEnabled = ingestionEnabled;
        RetrievalEnabled = retrievalEnabled;
        UpdatedAtUtc = now;
    }
}

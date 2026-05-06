namespace Callio.Core.Infrastructure.Messaging.Knowledge;

public record RetrieveTenantGenerationSourcesRequest(
    int TenantId,
    string Input,
    IReadOnlyList<TenantGenerationDataSourceSelectionMessage> DataSources);

public record RetrieveTenantGenerationSourcesResponse(
    TenantKnowledgeConfigurationMessage Configuration,
    IReadOnlyList<RetrievedTenantGenerationSourceMessage> Sources);

public record TenantGenerationDataSourceSelectionMessage(
    string SourceKind,
    int? CategoryId,
    string? CategoryName,
    int? TagId,
    string? TagName,
    int? DocumentId,
    int? MaxChunks,
    bool IncludeBlobContent);

public record RetrievedTenantGenerationSourceMessage(
    string SourceKind,
    int? KnowledgeDocumentId,
    string? DocumentTitle,
    int? CategoryId,
    string? CategoryName,
    int? ChunkId,
    int? ChunkIndex,
    decimal? Score,
    string? BlobContainerName,
    string? BlobName,
    string? BlobUri,
    string Content);

public record TenantKnowledgeConfigurationMessage(
    int Id,
    int TenantId,
    string SystemPrompt,
    string AssistantInstructionPrompt,
    int ChunkSize,
    int ChunkOverlap,
    int TopKRetrievalCount,
    int MaximumChunksInFinalContext,
    decimal MinimumSimilarityThreshold,
    IReadOnlyList<string> AllowedFileTypes,
    long MaximumFileSizeBytes,
    bool AutoProcessOnUpload,
    bool ManualApprovalRequiredBeforeIndexing,
    bool VersioningEnabled,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    TenantKnowledgeModelConstraintsMessage Models);

public record TenantKnowledgeModelConstraintsMessage(
    string EmbeddingProvider,
    string EmbeddingModel,
    string GenerationModel);

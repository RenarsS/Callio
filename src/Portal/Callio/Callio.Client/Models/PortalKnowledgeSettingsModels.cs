namespace Callio.Client.Models;

public record PortalKnowledgeModelsResponse(
    string EmbeddingProvider,
    string EmbeddingModel,
    string GenerationModel);

public record PortalTenantKnowledgeSettingsResponse(
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
    PortalKnowledgeModelsResponse Models);

public record PortalTenantKnowledgeSetupStatusResponse(
    int TenantId,
    string Status,
    int AttemptCount,
    int? ActiveConfigurationId,
    string? LastError,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? LastStartedAtUtc,
    DateTime? LastCompletedAtUtc);

namespace Callio.Provisioning.Application;

public record TenantApprovedProvisioningCommand(
    string UserId,
    int TenantId,
    int TenantRequestId);

public record TenantProvisioningStatusDto(
    int TenantId,
    int TenantRequestId,
    string RequestedByUserId,
    string Status,
    int AttemptCount,
    string DatabaseSchema,
    string VectorStoreNamespace,
    string? FailedStep,
    string? LastError,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? LastStartedAtUtc,
    DateTime? LastCompletedAtUtc,
    TenantKnowledgeBaseSettingsDto? Settings,
    IReadOnlyList<TenantProvisioningStepDto> Steps);

public record TenantProvisioningStepDto(
    string Name,
    int Order,
    string Status,
    int AttemptCount,
    string? LastError,
    DateTime? LastStartedAtUtc,
    DateTime? LastCompletedAtUtc);

public record TenantKnowledgeBaseSettingsDto(
    string DatabaseSchema,
    string VectorStoreNamespace,
    string EmbeddingProvider,
    string EmbeddingModel,
    int ChunkSize,
    int ChunkOverlap,
    int RetrievalTopK,
    bool IngestionEnabled,
    bool RetrievalEnabled,
    DateTime UpdatedAtUtc);

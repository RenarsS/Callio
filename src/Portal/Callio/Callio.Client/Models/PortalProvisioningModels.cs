namespace Callio.Client.Models;

public record PortalTenantProvisioningKnowledgeSetupResponse(
    int TenantId,
    string Status,
    int AttemptCount,
    int? ActiveConfigurationId,
    string? LastError,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? LastStartedAtUtc,
    DateTime? LastCompletedAtUtc);

public record PortalTenantProvisioningSettingsSummaryResponse(
    int Id,
    int TenantId,
    string EmbeddingProvider,
    string EmbeddingModel,
    string GenerationModel,
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
    DateTime UpdatedAtUtc);

public record PortalTenantProvisioningStepResponse(
    string Name,
    int Order,
    string Status,
    int AttemptCount,
    string? LastError,
    DateTime? LastStartedAtUtc,
    DateTime? LastCompletedAtUtc);

public record PortalTenantProvisioningStatusResponse(
    int TenantId,
    int TenantRequestId,
    string RequestedByUserId,
    string Status,
    int AttemptCount,
    string DatabaseSchema,
    string VectorStoreNamespace,
    string BlobContainerName,
    string? FailedStep,
    string? LastError,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? LastStartedAtUtc,
    DateTime? LastCompletedAtUtc,
    PortalTenantProvisioningKnowledgeSetupResponse? KnowledgeConfigurationSetup,
    PortalTenantProvisioningSettingsSummaryResponse? Settings,
    IReadOnlyList<PortalTenantProvisioningStepResponse> Steps);

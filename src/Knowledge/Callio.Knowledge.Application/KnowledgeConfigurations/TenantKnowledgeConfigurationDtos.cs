using Callio.Knowledge.Domain;
using Callio.Knowledge.Domain.Enums;

namespace Callio.Knowledge.Application.KnowledgeConfigurations;

public record CreateDefaultTenantKnowledgeConfigurationCommand(int TenantId);

public record RunTenantKnowledgeConfigurationSetupCommand(int TenantId);

public record ChangeTenantKnowledgeConfigurationStatusCommand(int TenantId, int ConfigurationId);

public record UpdateTenantKnowledgeConfigurationCommand(
    int TenantId,
    int ConfigurationId,
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
    bool VersioningEnabled);

public record KnowledgeModelConstraintsDto(
    string EmbeddingProvider,
    string EmbeddingModel,
    string GenerationModel);

public record TenantKnowledgeConfigurationDto(
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
    KnowledgeModelConstraintsDto Models);

public record TenantKnowledgeConfigurationSetupStatusDto(
    int TenantId,
    string Status,
    int AttemptCount,
    int? ActiveConfigurationId,
    string? LastError,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? LastStartedAtUtc,
    DateTime? LastCompletedAtUtc);

public record TenantKnowledgeConfigurationSummaryDto(
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

public static class TenantKnowledgeConfigurationMappings
{
    public static TenantKnowledgeConfigurationSetupStatusDto ToDto(this TenantKnowledgeConfigurationSetup setup)
        => new(
            setup.TenantId,
            setup.Status.ToString(),
            setup.AttemptCount,
            setup.ActiveConfigurationId,
            setup.LastError,
            setup.CreatedAtUtc,
            setup.UpdatedAtUtc,
            setup.LastStartedAtUtc,
            setup.LastCompletedAtUtc);

    public static TenantKnowledgeConfigurationDto ToDto(
        this TenantKnowledgeConfiguration configuration,
        KnowledgeModelConstraintsDto models)
        => new(
            configuration.Id,
            configuration.TenantId,
            configuration.SystemPrompt,
            configuration.AssistantInstructionPrompt,
            configuration.ChunkSize,
            configuration.ChunkOverlap,
            configuration.TopKRetrievalCount,
            configuration.MaximumChunksInFinalContext,
            configuration.MinimumSimilarityThreshold,
            configuration.AllowedFileTypes.ToList(),
            configuration.MaximumFileSizeBytes,
            configuration.AutoProcessOnUpload,
            configuration.ManualApprovalRequiredBeforeIndexing,
            configuration.VersioningEnabled,
            configuration.IsActive,
            configuration.CreatedAtUtc,
            configuration.UpdatedAtUtc,
            models);

    public static TenantKnowledgeConfigurationSummaryDto ToSummaryDto(
        this TenantKnowledgeConfiguration configuration,
        KnowledgeModelConstraintsDto models)
        => new(
            configuration.Id,
            configuration.TenantId,
            models.EmbeddingProvider,
            models.EmbeddingModel,
            models.GenerationModel,
            configuration.ChunkSize,
            configuration.ChunkOverlap,
            configuration.TopKRetrievalCount,
            configuration.MaximumChunksInFinalContext,
            configuration.MinimumSimilarityThreshold,
            configuration.AllowedFileTypes.ToList(),
            configuration.MaximumFileSizeBytes,
            configuration.AutoProcessOnUpload,
            configuration.ManualApprovalRequiredBeforeIndexing,
            configuration.VersioningEnabled,
            configuration.IsActive,
            configuration.UpdatedAtUtc);

    public static TenantKnowledgeConfigurationSummaryDto ToSummaryDto(this TenantKnowledgeConfigurationDto configuration)
        => new(
            configuration.Id,
            configuration.TenantId,
            configuration.Models.EmbeddingProvider,
            configuration.Models.EmbeddingModel,
            configuration.Models.GenerationModel,
            configuration.ChunkSize,
            configuration.ChunkOverlap,
            configuration.TopKRetrievalCount,
            configuration.MaximumChunksInFinalContext,
            configuration.MinimumSimilarityThreshold,
            configuration.AllowedFileTypes.ToList(),
            configuration.MaximumFileSizeBytes,
            configuration.AutoProcessOnUpload,
            configuration.ManualApprovalRequiredBeforeIndexing,
            configuration.VersioningEnabled,
            configuration.IsActive,
            configuration.UpdatedAtUtc);
}

using Callio.Knowledge.Application.KnowledgeConfigurations;

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
    string BlobContainerName,
    string? FailedStep,
    string? LastError,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? LastStartedAtUtc,
    DateTime? LastCompletedAtUtc,
    TenantKnowledgeConfigurationSetupStatusDto? KnowledgeConfigurationSetup,
    TenantKnowledgeConfigurationSummaryDto? Settings,
    IReadOnlyList<TenantProvisioningStepDto> Steps);

public record TenantProvisioningStepDto(
    string Name,
    int Order,
    string Status,
    int AttemptCount,
    string? LastError,
    DateTime? LastStartedAtUtc,
    DateTime? LastCompletedAtUtc);

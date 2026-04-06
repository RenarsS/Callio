namespace Callio.Core.Infrastructure.Messaging.Tenants;

public record TenantProvisioningStepIntegrationStatus(
    string StepName,
    string Status,
    int AttemptCount,
    string? ErrorMessage);

public record TenantInfrastructureProvisioningSucceededIntegrationEvent(
    int TenantId,
    int TenantRequestId,
    string RequestedByUserId,
    string DatabaseSchema,
    string VectorStoreNamespace,
    int AttemptCount,
    IReadOnlyList<TenantProvisioningStepIntegrationStatus> Steps,
    DateTime OccurredAtUtc);

public record TenantInfrastructureProvisioningFailedIntegrationEvent(
    int TenantId,
    int TenantRequestId,
    string RequestedByUserId,
    string DatabaseSchema,
    string VectorStoreNamespace,
    int AttemptCount,
    string? FailedStep,
    string? ErrorMessage,
    IReadOnlyList<TenantProvisioningStepIntegrationStatus> Steps,
    DateTime OccurredAtUtc);

namespace Callio.Core.Infrastructure.Messaging.Tenants;

public record TenantApprovedIntegrationEvent(
    string UserId,
    int TenantId,
    int TenantRequestId);

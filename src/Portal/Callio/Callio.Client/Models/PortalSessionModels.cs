namespace Callio.Client.Models;

public record PortalUserSession(
    string AccessToken,
    string UserId,
    string Email,
    string DisplayName,
    string UserType,
    int? TenantId,
    DateTime? ExpiresAtUtc);

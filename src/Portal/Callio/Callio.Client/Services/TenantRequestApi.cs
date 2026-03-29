using System.Net.Http.Json;

namespace Callio.Client.Services;

public class TenantRequestApi(HttpClient httpClient)
{
    public async Task<PortalRegistrationResponse> RegisterUserAndTenantAsync(RegisterPortalUserAndTenantRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/portal/onboarding/register-tenant", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PortalRegistrationResponse>(cancellationToken)
               ?? throw new InvalidOperationException("Portal onboarding response was empty.");
    }
}

public record RegisterPortalUserAndTenantRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string CompanyName,
    string TenantName,
    string? Notes);

public record PortalRegistrationResponse(
    string UserId,
    int TenantRequestId,
    string Email,
    string TenantName,
    string CompanyName,
    string Status,
    string Message);
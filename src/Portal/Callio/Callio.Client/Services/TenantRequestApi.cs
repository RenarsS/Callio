using System.Net.Http.Json;
using Callio.Client.Models;

namespace Callio.Client.Services;

public class TenantRequestApi(HttpClient httpClient)
{
    public async Task<IReadOnlyList<PortalPlanResponse>> GetPlansAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<List<PortalPlanResponse>>("/api/portal/plans", cancellationToken) ?? [];

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
    int? RequestedPlanId,
    string? RequestedPlanName,
    string? Notes);

public record PortalRegistrationResponse(
    string UserId,
    int TenantRequestId,
    string Email,
    string TenantName,
    string CompanyName,
    string Status,
    string Message);
using System.Net.Http.Json;
using Callio.Client.Models;

namespace Callio.Client.Services;

public class TenantRequestApi(HttpClient httpClient)
{
    public async Task<IReadOnlyList<PortalPlanResponse>> GetPlansAsync(CancellationToken cancellationToken = default)
        => await httpClient.GetFromJsonAsync<List<PortalPlanResponse>>("/api/portal/plans", cancellationToken) ?? [];

    public async Task<PortalRegistrationResponse> RegisterUserAndTenantAsync(RegisterPortalUserAndTenantRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/portal/tenant-onboarding", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<PortalRegistrationResponse>(cancellationToken)
               ?? throw new InvalidOperationException("Portal onboarding response was empty.");
    }

    public async Task<PortalTenantRequestStatusResponse> GetRequestStatusAsync(int requestId, string email, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"/api/portal/tenant-requests/{requestId}?email={Uri.EscapeDataString(email)}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<PortalTenantRequestStatusResponse>(cancellationToken)
               ?? throw new InvalidOperationException("Tenant request status response was empty.");
    }

    public async Task<PortalTenantRequestStatusResponse> GetRequestStatusByTenantIdAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"/api/portal/tenant-requests/by-tenant/{tenantId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<PortalTenantRequestStatusResponse>(cancellationToken)
               ?? throw new InvalidOperationException("Tenant request status response was empty.");
    }

    private static Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        => PortalApiResponseHelper.EnsureSuccessAsync(response, cancellationToken);
}

public record RegisterPortalUserAndTenantRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string CompanyName,
    string TenantName,
    int? SelectedPlanId,
    string? Notes);

public record PortalRegistrationResponse(
    string UserId,
    int TenantRequestId,
    string Email,
    string TenantName,
    string CompanyName,
    string Status,
    string Message,
    int? SelectedPlanId,
    string? SelectedPlanName);

public record PortalTenantRequestStatusResponse(
    int Id,
    string TenantName,
    string CompanyName,
    string RequestedByEmail,
    string RequestedByFullName,
    string Status,
    DateTime RequestedAtUtc,
    DateTime? ProcessedAtUtc,
    string? DecisionNote,
    int? TenantId,
    int? SelectedPlanId,
    string? SelectedPlanName);

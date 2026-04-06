using System.Net.Http.Json;
using System.Text.Json;

namespace Callio.Client.Services;

public class TenantRequestApi(HttpClient httpClient)
{
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

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var raw = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException(response.ReasonPhrase ?? "The request failed.");

        if (raw.StartsWith("\"", StringComparison.Ordinal))
        {
            var message = JsonSerializer.Deserialize<string>(raw);
            throw new InvalidOperationException(message ?? raw);
        }

        if (raw.StartsWith("{", StringComparison.Ordinal))
        {
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;

            if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                throw new InvalidOperationException(detail.GetString() ?? raw);

            if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                throw new InvalidOperationException(message.GetString() ?? raw);

            if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                throw new InvalidOperationException(title.GetString() ?? raw);
        }

        throw new InvalidOperationException(raw);
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
    int? TenantId);

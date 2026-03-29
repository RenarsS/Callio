using System.Net.Http.Json;

namespace Callio.Client.Services;

public class TenantRequestApi(HttpClient httpClient)
{
    public async Task RegisterAsync(RegisterPortalUserRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/register", new
        {
            email = request.Email,
            password = request.Password,
            firstName = "N/A",
            lastName = "N/A",
        }, cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    public async Task CreateTenantRequestAsync(CreatePortalTenantRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/portal/tenant-requests", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

public record RegisterPortalUserRequest(string Email, string Password);

public record CreatePortalTenantRequest(
    string TenantName,
    string RequestedByUserId,
    string RequestedByEmail,
    string RequestedByFirstName,
    string RequestedByLastName,
    string CompanyName,
    string? Notes);
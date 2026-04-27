using System.Net;
using System.Net.Http.Json;
using Callio.Client.Models;

namespace Callio.Client.Services;

public class TenantProvisioningApi(HttpClient httpClient)
{
    public async Task<PortalTenantProvisioningStatusResponse?> GetStatusAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"/api/portal/tenants/{tenantId}/provisioning", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await PortalApiResponseHelper.EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<PortalTenantProvisioningStatusResponse>(cancellationToken)
               ?? throw new InvalidOperationException("Tenant provisioning response was empty.");
    }
}

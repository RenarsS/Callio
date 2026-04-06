using System.Net.Http.Json;
using System.Text.Json;

namespace Callio.Admin.Services;

public class AdminApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<TenantRequestItem>> GetTenantRequestsAsync(CancellationToken ct = default)
        => await httpClient.GetFromJsonAsync<List<TenantRequestItem>>("/api/dashboard/tenant-requests", ct) ?? [];

    public async Task<IReadOnlyList<TenantItem>> GetTenantsAsync(CancellationToken ct = default)
        => await httpClient.GetFromJsonAsync<List<TenantItem>>("/api/dashboard/tenants", ct) ?? [];

    public async Task<IReadOnlyList<PlanItem>> GetPlansAsync(CancellationToken ct = default)
        => await httpClient.GetFromJsonAsync<List<PlanItem>>("/api/dashboard/plans", ct) ?? [];

    public async Task<IReadOnlyList<UsageMetricItem>> GetUsageMetricsAsync(CancellationToken ct = default)
        => await httpClient.GetFromJsonAsync<List<UsageMetricItem>>("/api/dashboard/usage-metrics", ct) ?? [];

    public Task ApproveRequestAsync(int requestId, string note, CancellationToken ct = default)
        => PostAsync($"/api/dashboard/tenant-requests/{requestId}/approve", new ProcessTenantRequestRequest("dashboard-admin", note), ct);

    public Task RejectRequestAsync(int requestId, string note, CancellationToken ct = default)
        => PostAsync($"/api/dashboard/tenant-requests/{requestId}/reject", new ProcessTenantRequestRequest("dashboard-admin", note), ct);

    public Task CreatePlanAsync(PlanUpsertRequest request, CancellationToken ct = default) => PostAsync("/api/dashboard/plans", request, ct);
    public Task UpdatePlanAsync(int id, PlanUpsertRequest request, CancellationToken ct = default) => PutAsync($"/api/dashboard/plans/{id}", request, ct);
    public Task DeletePlanAsync(int id, CancellationToken ct = default) => DeleteAsync($"/api/dashboard/plans/{id}", ct);

    public Task CreateUsageMetricAsync(UsageMetricUpsertRequest request, CancellationToken ct = default) => PostAsync("/api/dashboard/usage-metrics", request, ct);
    public Task UpdateUsageMetricAsync(int id, UsageMetricUpsertRequest request, CancellationToken ct = default) => PutAsync($"/api/dashboard/usage-metrics/{id}", request, ct);
    public Task DeleteUsageMetricAsync(int id, CancellationToken ct = default) => DeleteAsync($"/api/dashboard/usage-metrics/{id}", ct);

    public Task CreateQuotaAsync(int planId, QuotaUpsertRequest request, CancellationToken ct = default) => PostAsync($"/api/dashboard/plans/{planId}/quotas", request, ct);
    public Task UpdateQuotaAsync(int planId, int quotaId, QuotaUpdateRequest request, CancellationToken ct = default) => PutAsync($"/api/dashboard/plans/{planId}/quotas/{quotaId}", request, ct);
    public Task DeleteQuotaAsync(int planId, int quotaId, CancellationToken ct = default) => DeleteAsync($"/api/dashboard/plans/{planId}/quotas/{quotaId}", ct);

    private async Task PostAsync<T>(string uri, T payload, CancellationToken ct)
    {
        var response = await httpClient.PostAsJsonAsync(uri, payload, ct);
        await EnsureSuccessAsync(response, ct);
    }

    private async Task PutAsync<T>(string uri, T payload, CancellationToken ct)
    {
        var response = await httpClient.PutAsJsonAsync(uri, payload, ct);
        await EnsureSuccessAsync(response, ct);
    }

    private async Task DeleteAsync(string uri, CancellationToken ct)
    {
        var response = await httpClient.DeleteAsync(uri, ct);
        await EnsureSuccessAsync(response, ct);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        var raw = (await response.Content.ReadAsStringAsync(ct)).Trim();
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
        }

        throw new InvalidOperationException(raw);
    }
}

public record TenantRequestItem(
    int Id,
    string TenantName,
    string CompanyName,
    string RequestedByEmail,
    string RequestedByFullName,
    int Status,
    DateTime RequestedAtUtc,
    DateTime? ProcessedAtUtc,
    string? Notes,
    string? DecisionNote,
    string? ProcessedByUserId,
    int? TenantId,
    int? SelectedPlanId,
    string? SelectedPlanName);

public record TenantItem(int Id, string Name, Guid? TenantCode, string ContactName, string ContactEmail, DateTime CreatedAt, DateTime ActivatedAt, DateTime? DeactivatedAt, string Status, string? CurrentPlanName, string? SubscriptionStatus);
public record PlanItem(int Id, string Name, string Description, decimal BasePrice, string Currency, int BillingInterval, int AnchorDay, bool IsActive, IReadOnlyList<PlanQuotaItem> Quotas);
public record PlanQuotaItem(int Id, int UsageMetricId, string UsageMetricKey, string UsageMetricDisplayName, string Unit, decimal Limit, bool HardLimit, decimal? OverageUnitPrice, string? OverageCurrency);
public record UsageMetricItem(int Id, string Key, string DisplayName, string Unit, int Type);

public record ProcessTenantRequestRequest(string ProcessedByUserId, string? DecisionNote);
public record PlanUpsertRequest(string Name, string Description, decimal BasePrice, string Currency, int BillingInterval, int AnchorDay, bool IsActive);
public record UsageMetricUpsertRequest(string Key, string DisplayName, string Unit, int Type);
public record QuotaUpsertRequest(int UsageMetricId, decimal Limit, bool HardLimit, decimal? OverageUnitPrice, string? Currency);
public record QuotaUpdateRequest(decimal Limit, bool HardLimit, decimal? OverageUnitPrice, string? Currency);

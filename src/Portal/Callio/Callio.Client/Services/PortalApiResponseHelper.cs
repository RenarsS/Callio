using System.Text.Json;

namespace Callio.Client.Services;

internal static class PortalApiResponseHelper
{
    public static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
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

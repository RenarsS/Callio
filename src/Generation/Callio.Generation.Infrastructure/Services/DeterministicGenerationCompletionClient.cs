using Callio.Generation.Application.Generation;

namespace Callio.Generation.Infrastructure.Services;

public class DeterministicGenerationCompletionClient : IGenerationCompletionClient
{
    public Task<GenerationCompletionResultDto> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        string model,
        CancellationToken cancellationToken = default)
    {
        var response = $"""
Generation provider is running in deterministic mode.

The final prompt was composed successfully and is ready to be sent to the configured generation provider.

Prompt preview:
{CreatePreview(userPrompt, 1200)}
""";

        return Task.FromResult(new GenerationCompletionResultDto(
            response,
            string.IsNullOrWhiteSpace(model) ? "deterministic" : model,
            EstimateTokens(response)));
    }

    private static string CreatePreview(string value, int maxLength)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length <= maxLength)
            return normalized;

        return normalized[..maxLength].TrimEnd() + "...";
    }

    private static int EstimateTokens(string value)
        => Math.Max(1, (int)Math.Ceiling((value?.Length ?? 0) / 4d));
}

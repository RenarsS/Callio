using Callio.Generation.Application.Generation;
using Callio.Generation.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Callio.Generation.Infrastructure.Services;

public class TenantGenerationCompletionClient(
    IOptions<TenantGenerationOptions> options,
    DeterministicGenerationCompletionClient deterministicClient,
    OpenAiGenerationCompletionClient openAiClient) : IGenerationCompletionClient
{
    private readonly TenantGenerationOptions _options = options.Value;

    public Task<GenerationCompletionResultDto> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        string model,
        CancellationToken cancellationToken = default)
        => UseOpenAi()
            ? openAiClient.CompleteAsync(systemPrompt, userPrompt, model, cancellationToken)
            : deterministicClient.CompleteAsync(systemPrompt, userPrompt, model, cancellationToken);

    private bool UseOpenAi()
        => string.Equals(_options.CompletionProvider, "OpenAI", StringComparison.OrdinalIgnoreCase);
}

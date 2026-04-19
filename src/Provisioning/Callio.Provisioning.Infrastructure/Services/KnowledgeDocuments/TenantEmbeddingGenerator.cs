using Callio.Provisioning.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Callio.Provisioning.Infrastructure.Services.KnowledgeDocuments;

public class TenantEmbeddingGenerator(
    IOptions<TenantKnowledgeIngestionOptions> options,
    DeterministicTenantEmbeddingGenerator deterministicGenerator,
    AzureOpenAiTenantEmbeddingGenerator azureOpenAiGenerator) : ITenantEmbeddingGenerator
{
    private readonly TenantKnowledgeIngestionOptions _options = options.Value;

    public Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> chunks,
        string embeddingModel,
        CancellationToken cancellationToken = default)
        => UseAzureOpenAi()
            ? azureOpenAiGenerator.GenerateEmbeddingsAsync(chunks, embeddingModel, cancellationToken)
            : deterministicGenerator.GenerateEmbeddingsAsync(chunks, embeddingModel, cancellationToken);

    private bool UseAzureOpenAi()
        => string.Equals(_options.EmbeddingProvider, "AzureOpenAI", StringComparison.OrdinalIgnoreCase);
}

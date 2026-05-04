using Callio.Knowledge.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Callio.Knowledge.Infrastructure.Services.KnowledgeDocuments;

public class TenantEmbeddingGenerator(
    IOptions<TenantKnowledgeIngestionOptions> options,
    DeterministicTenantEmbeddingGenerator deterministicGenerator,
    OpenAiTenantEmbeddingGenerator openAiGenerator) : ITenantEmbeddingGenerator
{
    private readonly TenantKnowledgeIngestionOptions _options = options.Value;

    public Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> chunks,
        string embeddingModel,
        CancellationToken cancellationToken = default)
        => UseOpenAi()
            ? openAiGenerator.GenerateEmbeddingsAsync(chunks, embeddingModel, cancellationToken)
            : deterministicGenerator.GenerateEmbeddingsAsync(chunks, embeddingModel, cancellationToken);

    private bool UseOpenAi()
        => string.Equals(_options.EmbeddingProvider, "OpenAI", StringComparison.OrdinalIgnoreCase);
}

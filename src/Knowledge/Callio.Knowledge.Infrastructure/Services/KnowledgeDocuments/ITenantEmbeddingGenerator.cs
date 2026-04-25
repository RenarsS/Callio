namespace Callio.Knowledge.Infrastructure.Services.KnowledgeDocuments;

public interface ITenantEmbeddingGenerator
{
    Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> chunks,
        string embeddingModel,
        CancellationToken cancellationToken = default);
}

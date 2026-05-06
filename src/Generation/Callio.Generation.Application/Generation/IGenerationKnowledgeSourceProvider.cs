namespace Callio.Generation.Application.Generation;

public interface IGenerationKnowledgeSourceProvider
{
    Task<TenantGenerationKnowledgeContextDto> RetrieveAsync(
        int tenantId,
        string input,
        IReadOnlyList<GenerationDataSourceSelectionDto> dataSources,
        CancellationToken cancellationToken = default);
}

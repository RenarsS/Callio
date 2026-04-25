using Callio.Knowledge.Application.KnowledgeConfigurations;

namespace Callio.Generation.Application.Generation;

public interface IGenerationKnowledgeSourceProvider
{
    Task<IReadOnlyList<RetrievedGenerationSourceDto>> RetrieveAsync(
        int tenantId,
        string input,
        TenantKnowledgeConfigurationDto configuration,
        IReadOnlyList<GenerationDataSourceSelectionDto> dataSources,
        CancellationToken cancellationToken = default);
}

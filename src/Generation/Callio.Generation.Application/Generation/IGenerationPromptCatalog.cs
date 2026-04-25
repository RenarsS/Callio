namespace Callio.Generation.Application.Generation;

public interface IGenerationPromptCatalog
{
    Task<IReadOnlyList<GenerationPromptTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default);

    Task<GenerationPromptTemplateDto> GetTemplateAsync(string? promptKey, CancellationToken cancellationToken = default);
}

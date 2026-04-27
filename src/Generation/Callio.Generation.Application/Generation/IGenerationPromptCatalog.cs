namespace Callio.Generation.Application.Generation;

public interface IGenerationPromptCatalog
{
    Task<IReadOnlyList<GenerationPromptTemplateDto>> GetTemplatesAsync(int tenantId, CancellationToken cancellationToken = default);

    Task<GenerationPromptTemplateDto> GetTemplateAsync(int tenantId, string? promptKey, CancellationToken cancellationToken = default);

    Task<GenerationPromptTemplateDto> CreateTemplateAsync(
        CreateTenantGenerationPromptTemplateCommand command,
        CancellationToken cancellationToken = default);

    Task<GenerationPromptTemplateDto?> UpdateTemplateAsync(
        UpdateTenantGenerationPromptTemplateCommand command,
        CancellationToken cancellationToken = default);
}

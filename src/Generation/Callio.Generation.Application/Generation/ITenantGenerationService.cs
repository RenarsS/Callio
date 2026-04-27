namespace Callio.Generation.Application.Generation;

public interface ITenantGenerationService
{
    Task<IReadOnlyList<GenerationPromptTemplateDto>> GetPromptTemplatesAsync(
        int tenantId,
        CancellationToken cancellationToken = default);

    Task<GenerationPromptTemplateDto> CreatePromptTemplateAsync(
        CreateTenantGenerationPromptTemplateCommand command,
        CancellationToken cancellationToken = default);

    Task<GenerationPromptTemplateDto?> UpdatePromptTemplateAsync(
        UpdateTenantGenerationPromptTemplateCommand command,
        CancellationToken cancellationToken = default);

    Task<GenerationResponseDto> GenerateAsync(
        GenerateTenantResponseCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GenerationResponseDto>> GetResponsesAsync(
        int tenantId,
        GetTenantGenerationResponsesQuery query,
        CancellationToken cancellationToken = default);

    Task<GenerationResponseDto?> GetResponseAsync(
        int tenantId,
        int responseId,
        CancellationToken cancellationToken = default);
}

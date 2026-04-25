namespace Callio.Generation.Application.Generation;

public interface ITenantGenerationService
{
    Task<IReadOnlyList<GenerationPromptTemplateDto>> GetPromptTemplatesAsync(CancellationToken cancellationToken = default);

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

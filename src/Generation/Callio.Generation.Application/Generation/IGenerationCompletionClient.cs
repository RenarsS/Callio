namespace Callio.Generation.Application.Generation;

public interface IGenerationCompletionClient
{
    Task<GenerationCompletionResultDto> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        string model,
        CancellationToken cancellationToken = default);
}

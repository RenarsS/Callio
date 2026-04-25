using Callio.Generation.Domain;

namespace Callio.Generation.Application.Generation;

public record GenerateTenantResponseCommand(
    int TenantId,
    string Input,
    string? PromptKey,
    IReadOnlyList<GenerationDataSourceSelectionDto> DataSources,
    IReadOnlyDictionary<string, string> Variables,
    bool SaveResponse,
    string? RequestedByUserId,
    string? RequestedByDisplayName);

public record GenerationDataSourceSelectionDto(
    string SourceKind,
    int? CategoryId,
    string? CategoryName,
    int? TagId,
    string? TagName,
    int? DocumentId,
    int? MaxChunks,
    bool IncludeBlobContent);

public record GenerationPromptTemplateDto(
    string Key,
    string Name,
    string? Description,
    string SystemPrompt,
    string UserPromptTemplate,
    IReadOnlyList<GenerationDataSourceSelectionDto> DataSources);

public record RetrievedGenerationSourceDto(
    string SourceKind,
    int? KnowledgeDocumentId,
    string? DocumentTitle,
    int? CategoryId,
    string? CategoryName,
    int? ChunkId,
    int? ChunkIndex,
    decimal? Score,
    string? BlobContainerName,
    string? BlobName,
    string? BlobUri,
    string Content);

public record GenerationPromptCompositionDto(
    string SystemPrompt,
    string UserPrompt,
    string FinalPrompt,
    IReadOnlyList<RetrievedGenerationSourceDto> Sources,
    int EstimatedInputTokens);

public record GenerationCompletionResultDto(
    string ResponseText,
    string Model,
    int EstimatedOutputTokens);

public record GenerationResponseSourceDto(
    string SourceKind,
    int? KnowledgeDocumentId,
    string? DocumentTitle,
    int? CategoryId,
    string? CategoryName,
    int? ChunkId,
    int? ChunkIndex,
    decimal? Score,
    string? BlobContainerName,
    string? BlobName,
    string? BlobUri,
    string ContentExcerpt);

public record GenerationResponseDto(
    int? Id,
    Guid? ResponseKey,
    int TenantId,
    string PromptKey,
    string PromptName,
    string Input,
    string SystemPrompt,
    string UserPrompt,
    string FinalPrompt,
    string ResponseText,
    string GenerationModel,
    string Status,
    string? ErrorMessage,
    string? RequestedByUserId,
    string? RequestedByDisplayName,
    int EstimatedInputTokens,
    int EstimatedOutputTokens,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    IReadOnlyList<GenerationResponseSourceDto> Sources);

public record GetTenantGenerationResponsesQuery(int? Take);

public static class TenantGenerationMappings
{
    public static GenerationResponseDto ToDto(this TenantGenerationResponse response)
        => new(
            response.Id,
            response.ResponseKey,
            response.TenantId,
            response.PromptKey,
            response.PromptName,
            response.Input,
            response.SystemPrompt,
            response.UserPrompt,
            response.FinalPrompt,
            response.ResponseText,
            response.GenerationModel,
            response.Status.ToString(),
            response.ErrorMessage,
            response.RequestedByUserId,
            response.RequestedByDisplayName,
            response.EstimatedInputTokens,
            response.EstimatedOutputTokens,
            response.CreatedAtUtc,
            response.CompletedAtUtc,
            response.Sources
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.KnowledgeDocumentId)
                .ThenBy(x => x.ChunkIndex)
                .Select(x => new GenerationResponseSourceDto(
                    x.SourceKind.ToString(),
                    x.KnowledgeDocumentId,
                    x.DocumentTitle,
                    x.CategoryId,
                    x.CategoryName,
                    x.ChunkId,
                    x.ChunkIndex,
                    x.Score,
                    x.BlobContainerName,
                    x.BlobName,
                    x.BlobUri,
                    x.ContentExcerpt))
                .ToList());
}

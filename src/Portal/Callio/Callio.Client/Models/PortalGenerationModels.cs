namespace Callio.Client.Models;

public record PortalGenerationDataSourceResponse(
    string SourceKind,
    int? CategoryId,
    string? CategoryName,
    int? TagId,
    string? TagName,
    int? DocumentId,
    int? MaxChunks,
    bool IncludeBlobContent);

public record PortalGenerationPromptResponse(
    int Id,
    int TenantId,
    string Key,
    string Name,
    string? Description,
    string SystemPrompt,
    string UserPromptTemplate,
    IReadOnlyList<PortalGenerationDataSourceResponse> DataSources,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record PortalGenerationResponseSourceResponse(
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

public record PortalGenerationResponse(
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
    IReadOnlyList<PortalGenerationResponseSourceResponse> Sources);

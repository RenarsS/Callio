using Callio.Provisioning.Domain;
using Callio.Provisioning.Domain.Enums;

namespace Callio.Provisioning.Application.KnowledgeDocuments;

public record CreateTenantKnowledgeCategoryCommand(int TenantId, string Name, string? Description);

public record CreateTenantKnowledgeTagCommand(int TenantId, string Name);

public record UploadTenantKnowledgeDocumentCommand(
    int TenantId,
    string? Title,
    string FileName,
    string ContentType,
    byte[] Content,
    int? CategoryId,
    string? CategoryName,
    IReadOnlyList<int> TagIds,
    IReadOnlyList<string> TagNames,
    string? UploadedByUserId,
    string? UploadedByDisplayName,
    bool ApproveForIndexing,
    KnowledgeDocumentSourceType SourceType);

public record GetTenantKnowledgeDocumentsQuery(
    int? CategoryId,
    int? TagId,
    string? Status);

public record TenantKnowledgeCategoryDto(
    int Id,
    int TenantId,
    string Name,
    string? Description,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record TenantKnowledgeTagDto(
    int Id,
    int TenantId,
    string Name,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record TenantKnowledgeDocumentDto(
    int Id,
    Guid DocumentKey,
    int TenantId,
    int KnowledgeConfigurationId,
    string Title,
    string OriginalFileName,
    string ContentType,
    string FileExtension,
    long SizeBytes,
    string BlobContainerName,
    string BlobName,
    string BlobUri,
    string ContentHash,
    string VectorStoreNamespace,
    string SourceType,
    string ProcessingStatus,
    int ChunkCount,
    string? UploadedByUserId,
    string? UploadedByDisplayName,
    string? LastError,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? IndexedAtUtc,
    TenantKnowledgeCategoryDto? Category,
    IReadOnlyList<TenantKnowledgeTagDto> Tags);

public static class TenantKnowledgeDocumentMappings
{
    public static TenantKnowledgeCategoryDto ToDto(this TenantKnowledgeCategory category)
        => new(
            category.Id,
            category.TenantId,
            category.Name,
            category.Description,
            category.CreatedAtUtc,
            category.UpdatedAtUtc);

    public static TenantKnowledgeTagDto ToDto(this TenantKnowledgeTag tag)
        => new(
            tag.Id,
            tag.TenantId,
            tag.Name,
            tag.CreatedAtUtc,
            tag.UpdatedAtUtc);

    public static TenantKnowledgeDocumentDto ToDto(this TenantKnowledgeDocument document)
        => new(
            document.Id,
            document.DocumentKey,
            document.TenantId,
            document.KnowledgeConfigurationId,
            document.Title,
            document.OriginalFileName,
            document.ContentType,
            document.FileExtension,
            document.SizeBytes,
            document.BlobContainerName,
            document.BlobName,
            document.BlobUri,
            document.ContentHash,
            document.VectorStoreNamespace,
            document.SourceType.ToString(),
            document.ProcessingStatus.ToString(),
            document.ChunkCount,
            document.UploadedByUserId,
            document.UploadedByDisplayName,
            document.LastError,
            document.CreatedAtUtc,
            document.UpdatedAtUtc,
            document.IndexedAtUtc,
            document.Category?.ToDto(),
            document.DocumentTags
                .Select(x => x.Tag.ToDto())
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList());

    public static KnowledgeDocumentProcessingStatus? ParseStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Enum.TryParse<KnowledgeDocumentProcessingStatus>(value.Trim(), true, out var status)
            ? status
            : null;
    }
}

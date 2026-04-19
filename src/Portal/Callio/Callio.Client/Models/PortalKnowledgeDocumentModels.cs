namespace Callio.Client.Models;

public record PortalKnowledgeCategoryResponse(
    int Id,
    int TenantId,
    string Name,
    string? Description,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record PortalKnowledgeTagResponse(
    int Id,
    int TenantId,
    string Name,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record PortalKnowledgeDocumentResponse(
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
    PortalKnowledgeCategoryResponse? Category,
    IReadOnlyList<PortalKnowledgeTagResponse> Tags);

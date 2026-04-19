using Callio.Core.Domain.Exceptions;
using Callio.Core.Domain.Helpers;
using Callio.Provisioning.Domain.Enums;

namespace Callio.Provisioning.Domain;

public class TenantKnowledgeDocument : Entity<int>
{
    private const int MaxTitleLength = 256;
    private const int MaxFileNameLength = 260;
    private const int MaxContentTypeLength = 256;
    private const int MaxFileExtensionLength = 32;
    private const int MaxBlobContainerNameLength = 128;
    private const int MaxBlobNameLength = 512;
    private const int MaxBlobUriLength = 2000;
    private const int MaxContentHashLength = 64;
    private const int MaxVectorNamespaceLength = 256;
    private const int MaxUploadedByUserIdLength = 128;
    private const int MaxUploadedByDisplayNameLength = 200;
    private const int MaxErrorLength = 4000;
    private const int MaxTagsPerDocument = 32;

    public Guid DocumentKey { get; private set; }

    public int TenantId { get; private set; }

    public int KnowledgeConfigurationId { get; private set; }

    public int? CategoryId { get; private set; }

    public TenantKnowledgeCategory? Category { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string OriginalFileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public string FileExtension { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public string BlobContainerName { get; private set; } = string.Empty;

    public string BlobName { get; private set; } = string.Empty;

    public string BlobUri { get; private set; } = string.Empty;

    public string ContentHash { get; private set; } = string.Empty;

    public string VectorStoreNamespace { get; private set; } = string.Empty;

    public KnowledgeDocumentSourceType SourceType { get; private set; }

    public KnowledgeDocumentProcessingStatus ProcessingStatus { get; private set; }

    public string? UploadedByUserId { get; private set; }

    public string? UploadedByDisplayName { get; private set; }

    public int ChunkCount { get; private set; }

    public string? LastError { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? IndexedAtUtc { get; private set; }

    public ICollection<TenantKnowledgeDocumentTag> DocumentTags { get; private set; } = [];

    public ICollection<TenantKnowledgeDocumentChunk> Chunks { get; private set; } = [];

    private TenantKnowledgeDocument()
    {
    }

    public TenantKnowledgeDocument(
        int tenantId,
        int knowledgeConfigurationId,
        int? categoryId,
        string title,
        string originalFileName,
        string contentType,
        string fileExtension,
        long sizeBytes,
        string blobContainerName,
        string blobName,
        string blobUri,
        string contentHash,
        string vectorStoreNamespace,
        KnowledgeDocumentSourceType sourceType,
        string? uploadedByUserId,
        string? uploadedByDisplayName,
        DateTime now)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantId), "Tenant id must be greater than zero.");

        if (knowledgeConfigurationId <= 0)
            throw new ArgumentOutOfRangeException(nameof(knowledgeConfigurationId), "Knowledge configuration id must be greater than zero.");

        if (categoryId <= 0)
            throw new ArgumentOutOfRangeException(nameof(categoryId), "Category id must be greater than zero when provided.");

        if (sizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "File size must be greater than zero.");

        DocumentKey = Guid.NewGuid();
        TenantId = tenantId;
        KnowledgeConfigurationId = knowledgeConfigurationId;
        CategoryId = categoryId;
        Title = NormalizeRequired(title, MaxTitleLength, nameof(Title));
        OriginalFileName = NormalizeRequired(originalFileName, MaxFileNameLength, nameof(OriginalFileName));
        ContentType = NormalizeRequired(contentType, MaxContentTypeLength, nameof(ContentType));
        FileExtension = NormalizeFileExtension(fileExtension);
        SizeBytes = sizeBytes;
        BlobContainerName = NormalizeRequired(blobContainerName, MaxBlobContainerNameLength, nameof(BlobContainerName));
        BlobName = NormalizeRequired(blobName, MaxBlobNameLength, nameof(BlobName));
        BlobUri = NormalizeRequired(blobUri, MaxBlobUriLength, nameof(BlobUri));
        ContentHash = NormalizeRequired(contentHash, MaxContentHashLength, nameof(ContentHash));
        VectorStoreNamespace = NormalizeRequired(vectorStoreNamespace, MaxVectorNamespaceLength, nameof(VectorStoreNamespace));
        SourceType = sourceType;
        UploadedByUserId = NormalizeOptional(uploadedByUserId, MaxUploadedByUserIdLength, nameof(UploadedByUserId));
        UploadedByDisplayName = NormalizeOptional(uploadedByDisplayName, MaxUploadedByDisplayNameLength, nameof(UploadedByDisplayName));
        ProcessingStatus = KnowledgeDocumentProcessingStatus.Pending;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void AssignTags(IEnumerable<int> tagIds, DateTime now)
    {
        var normalized = (tagIds ?? [])
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        if (normalized.Count > MaxTagsPerDocument)
            throw new ArgumentOutOfRangeException(nameof(tagIds), $"No more than {MaxTagsPerDocument} tags may be assigned to a document.");

        DocumentTags.Clear();

        foreach (var tagId in normalized)
        {
            DocumentTags.Add(new TenantKnowledgeDocumentTag(tagId, now));
        }

        UpdatedAtUtc = now;
    }

    public void MarkAwaitingApproval(DateTime now)
    {
        ProcessingStatus = KnowledgeDocumentProcessingStatus.AwaitingApproval;
        ChunkCount = 0;
        LastError = null;
        IndexedAtUtc = null;
        UpdatedAtUtc = now;
    }

    public void MarkReady(IEnumerable<TenantKnowledgeDocumentChunk> chunks, DateTime now)
    {
        var normalizedChunks = (chunks ?? [])
            .OrderBy(x => x.ChunkIndex)
            .ToList();

        if (normalizedChunks.Count == 0)
            throw new InvalidFieldException(nameof(Chunks));

        Chunks.Clear();
        foreach (var chunk in normalizedChunks)
        {
            Chunks.Add(chunk);
        }

        ProcessingStatus = KnowledgeDocumentProcessingStatus.Ready;
        ChunkCount = normalizedChunks.Count;
        LastError = null;
        IndexedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void MarkFailed(string errorMessage, DateTime now)
    {
        Chunks.Clear();
        ProcessingStatus = KnowledgeDocumentProcessingStatus.Failed;
        ChunkCount = 0;
        IndexedAtUtc = null;
        LastError = NormalizeRequired(errorMessage, MaxErrorLength, nameof(LastError));
        UpdatedAtUtc = now;
    }

    private static string NormalizeRequired(string value, int maxLength, string fieldName)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
            throw new InvalidFieldException(fieldName);

        if (normalized.Length > maxLength)
            throw new ArgumentOutOfRangeException(fieldName, $"{fieldName} cannot exceed {maxLength} characters.");

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string fieldName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        if (normalized.Length > maxLength)
            throw new ArgumentOutOfRangeException(fieldName, $"{fieldName} cannot exceed {maxLength} characters.");

        return normalized;
    }

    private static string NormalizeFileExtension(string value)
    {
        var normalized = NormalizeRequired(value, MaxFileExtensionLength, nameof(FileExtension));
        if (!normalized.StartsWith('.'))
            normalized = $".{normalized}";

        if (normalized.Any(ch => !(char.IsLetterOrDigit(ch) || ch == '.')))
            throw new ArgumentOutOfRangeException(nameof(FileExtension), "File extension contains invalid characters.");

        return normalized.ToLowerInvariant();
    }
}

using Callio.Core.Domain.Exceptions;
using Callio.Core.Domain.Helpers;
using Callio.Generation.Domain.Enums;

namespace Callio.Generation.Domain;

public class TenantGenerationResponseSource : Entity<int>
{
    private const int MaxDocumentTitleLength = 256;
    private const int MaxCategoryNameLength = 120;
    private const int MaxBlobContainerNameLength = 128;
    private const int MaxBlobNameLength = 512;
    private const int MaxBlobUriLength = 2000;

    public int TenantGenerationResponseId { get; private set; }

    public TenantGenerationResponse Response { get; private set; } = null!;

    public GenerationSourceKind SourceKind { get; private set; }

    public int? KnowledgeDocumentId { get; private set; }

    public string? DocumentTitle { get; private set; }

    public int? CategoryId { get; private set; }

    public string? CategoryName { get; private set; }

    public int? ChunkId { get; private set; }

    public int? ChunkIndex { get; private set; }

    public decimal? Score { get; private set; }

    public string? BlobContainerName { get; private set; }

    public string? BlobName { get; private set; }

    public string? BlobUri { get; private set; }

    public string ContentExcerpt { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    private TenantGenerationResponseSource()
    {
    }

    public TenantGenerationResponseSource(
        GenerationSourceKind sourceKind,
        int? knowledgeDocumentId,
        string? documentTitle,
        int? categoryId,
        string? categoryName,
        int? chunkId,
        int? chunkIndex,
        decimal? score,
        string? blobContainerName,
        string? blobName,
        string? blobUri,
        string contentExcerpt,
        DateTime now)
    {
        if (knowledgeDocumentId <= 0)
            throw new ArgumentOutOfRangeException(nameof(knowledgeDocumentId), "Knowledge document id must be greater than zero when provided.");

        if (categoryId <= 0)
            throw new ArgumentOutOfRangeException(nameof(categoryId), "Category id must be greater than zero when provided.");

        if (chunkId <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkId), "Chunk id must be greater than zero when provided.");

        if (chunkIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(chunkIndex), "Chunk index cannot be negative.");

        SourceKind = sourceKind;
        KnowledgeDocumentId = knowledgeDocumentId;
        DocumentTitle = NormalizeOptional(documentTitle, MaxDocumentTitleLength, nameof(DocumentTitle));
        CategoryId = categoryId;
        CategoryName = NormalizeOptional(categoryName, MaxCategoryNameLength, nameof(CategoryName));
        ChunkId = chunkId;
        ChunkIndex = chunkIndex;
        Score = score;
        BlobContainerName = NormalizeOptional(blobContainerName, MaxBlobContainerNameLength, nameof(BlobContainerName));
        BlobName = NormalizeOptional(blobName, MaxBlobNameLength, nameof(BlobName));
        BlobUri = NormalizeOptional(blobUri, MaxBlobUriLength, nameof(BlobUri));
        ContentExcerpt = NormalizeRequired(contentExcerpt, int.MaxValue, nameof(ContentExcerpt));
        CreatedAtUtc = now;
    }

    private static string NormalizeRequired(string? value, int maxLength, string fieldName)
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
}

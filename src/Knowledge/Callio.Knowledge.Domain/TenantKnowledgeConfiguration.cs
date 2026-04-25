using Callio.Core.Domain.Exceptions;
using Callio.Core.Domain.Helpers;

namespace Callio.Knowledge.Domain;

public class TenantKnowledgeConfiguration : Entity<int>
{
    private const int MaxAllowedFileTypes = 32;

    public int TenantId { get; private set; }

    public string SystemPrompt { get; private set; } = string.Empty;

    public string AssistantInstructionPrompt { get; private set; } = string.Empty;

    public int ChunkSize { get; private set; }

    public int ChunkOverlap { get; private set; }

    public int TopKRetrievalCount { get; private set; }

    public int MaximumChunksInFinalContext { get; private set; }

    public decimal MinimumSimilarityThreshold { get; private set; }

    public List<string> AllowedFileTypes { get; private set; } = [];

    public long MaximumFileSizeBytes { get; private set; }

    public bool AutoProcessOnUpload { get; private set; }

    public bool ManualApprovalRequiredBeforeIndexing { get; private set; }

    public bool VersioningEnabled { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    private TenantKnowledgeConfiguration()
    {
    }

    public TenantKnowledgeConfiguration(
        int tenantId,
        string systemPrompt,
        string assistantInstructionPrompt,
        int chunkSize,
        int chunkOverlap,
        int topKRetrievalCount,
        int maximumChunksInFinalContext,
        decimal minimumSimilarityThreshold,
        IEnumerable<string> allowedFileTypes,
        long maximumFileSizeBytes,
        bool autoProcessOnUpload,
        bool manualApprovalRequiredBeforeIndexing,
        bool versioningEnabled,
        bool isActive,
        DateTime now)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantId), "Tenant id must be greater than zero.");

        TenantId = tenantId;
        Apply(
            systemPrompt,
            assistantInstructionPrompt,
            chunkSize,
            chunkOverlap,
            topKRetrievalCount,
            maximumChunksInFinalContext,
            minimumSimilarityThreshold,
            allowedFileTypes,
            maximumFileSizeBytes,
            autoProcessOnUpload,
            manualApprovalRequiredBeforeIndexing,
            versioningEnabled,
            now);

        IsActive = isActive;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void Update(
        string systemPrompt,
        string assistantInstructionPrompt,
        int chunkSize,
        int chunkOverlap,
        int topKRetrievalCount,
        int maximumChunksInFinalContext,
        decimal minimumSimilarityThreshold,
        IEnumerable<string> allowedFileTypes,
        long maximumFileSizeBytes,
        bool autoProcessOnUpload,
        bool manualApprovalRequiredBeforeIndexing,
        bool versioningEnabled,
        DateTime now)
    {
        Apply(
            systemPrompt,
            assistantInstructionPrompt,
            chunkSize,
            chunkOverlap,
            topKRetrievalCount,
            maximumChunksInFinalContext,
            minimumSimilarityThreshold,
            allowedFileTypes,
            maximumFileSizeBytes,
            autoProcessOnUpload,
            manualApprovalRequiredBeforeIndexing,
            versioningEnabled,
            now);
    }

    public void Activate(DateTime now)
    {
        IsActive = true;
        UpdatedAtUtc = now;
    }

    public void Deactivate(DateTime now)
    {
        IsActive = false;
        UpdatedAtUtc = now;
    }

    private void Apply(
        string systemPrompt,
        string assistantInstructionPrompt,
        int chunkSize,
        int chunkOverlap,
        int topKRetrievalCount,
        int maximumChunksInFinalContext,
        decimal minimumSimilarityThreshold,
        IEnumerable<string> allowedFileTypes,
        long maximumFileSizeBytes,
        bool autoProcessOnUpload,
        bool manualApprovalRequiredBeforeIndexing,
        bool versioningEnabled,
        DateTime now)
    {
        SystemPrompt = NormalizeRequired(systemPrompt, nameof(SystemPrompt));
        AssistantInstructionPrompt = NormalizeRequired(assistantInstructionPrompt, nameof(AssistantInstructionPrompt));
        ChunkSize = EnsureGreaterThanZero(chunkSize, nameof(ChunkSize));
        ChunkOverlap = EnsureZeroOrGreater(chunkOverlap, nameof(ChunkOverlap));

        if (ChunkOverlap >= ChunkSize)
            throw new ArgumentOutOfRangeException(nameof(ChunkOverlap), "Chunk overlap must be smaller than chunk size.");

        TopKRetrievalCount = EnsureGreaterThanZero(topKRetrievalCount, nameof(TopKRetrievalCount));
        MaximumChunksInFinalContext = EnsureGreaterThanZero(maximumChunksInFinalContext, nameof(MaximumChunksInFinalContext));

        if (MaximumChunksInFinalContext > TopKRetrievalCount)
            throw new ArgumentOutOfRangeException(nameof(MaximumChunksInFinalContext), "Maximum chunks in final context cannot exceed the top K retrieval count.");

        if (minimumSimilarityThreshold < 0m || minimumSimilarityThreshold > 1m)
            throw new ArgumentOutOfRangeException(nameof(minimumSimilarityThreshold), "Minimum similarity threshold must be between 0 and 1.");

        MinimumSimilarityThreshold = decimal.Round(minimumSimilarityThreshold, 4, MidpointRounding.AwayFromZero);
        AllowedFileTypes = NormalizeAllowedFileTypes(allowedFileTypes);
        MaximumFileSizeBytes = EnsureGreaterThanZero(maximumFileSizeBytes, nameof(MaximumFileSizeBytes));
        AutoProcessOnUpload = autoProcessOnUpload;
        ManualApprovalRequiredBeforeIndexing = manualApprovalRequiredBeforeIndexing;
        VersioningEnabled = versioningEnabled;
        UpdatedAtUtc = now;
    }

    private static string NormalizeRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidFieldException(fieldName);

        return value.Trim();
    }

    private static List<string> NormalizeAllowedFileTypes(IEnumerable<string> allowedFileTypes)
    {
        var normalized = (allowedFileTypes ?? [])
            .Select(x => (x ?? string.Empty).Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(NormalizeFileType)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count == 0)
            throw new InvalidFieldException(nameof(AllowedFileTypes));

        if (normalized.Count > MaxAllowedFileTypes)
            throw new ArgumentOutOfRangeException(nameof(AllowedFileTypes), $"No more than {MaxAllowedFileTypes} file types are allowed.");

        return normalized;
    }

    private static string NormalizeFileType(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith('.'))
            trimmed = trimmed[1..];

        if (trimmed.Length == 0 || trimmed.Any(ch => !char.IsLetterOrDigit(ch)))
            throw new ArgumentOutOfRangeException(nameof(AllowedFileTypes), "Allowed file types must be file extensions containing only letters and digits.");

        return $".{trimmed.ToLowerInvariant()}";
    }

    private static int EnsureGreaterThanZero(int value, string fieldName)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(fieldName, $"{fieldName} must be greater than zero.");

        return value;
    }

    private static long EnsureGreaterThanZero(long value, string fieldName)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(fieldName, $"{fieldName} must be greater than zero.");

        return value;
    }

    private static int EnsureZeroOrGreater(int value, string fieldName)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(fieldName, $"{fieldName} cannot be negative.");

        return value;
    }
}

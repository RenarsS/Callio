using Callio.Core.Domain.Exceptions;
using Callio.Core.Domain.Helpers;
using Callio.Generation.Domain.Enums;

namespace Callio.Generation.Domain;

public class TenantGenerationResponse : Entity<int>
{
    private const int MaxPromptKeyLength = 120;
    private const int MaxPromptNameLength = 200;
    private const int MaxGenerationModelLength = 256;
    private const int MaxRequestedByUserIdLength = 128;
    private const int MaxRequestedByDisplayNameLength = 200;
    private const int MaxErrorMessageLength = 4000;

    public Guid ResponseKey { get; private set; }

    public int TenantId { get; private set; }

    public string PromptKey { get; private set; } = string.Empty;

    public string PromptName { get; private set; } = string.Empty;

    public string Input { get; private set; } = string.Empty;

    public string SystemPrompt { get; private set; } = string.Empty;

    public string UserPrompt { get; private set; } = string.Empty;

    public string FinalPrompt { get; private set; } = string.Empty;

    public string ResponseText { get; private set; } = string.Empty;

    public string GenerationModel { get; private set; } = string.Empty;

    public GenerationResponseStatus Status { get; private set; }

    public string? ErrorMessage { get; private set; }

    public string? RequestedByUserId { get; private set; }

    public string? RequestedByDisplayName { get; private set; }

    public int SourceCount { get; private set; }

    public int EstimatedInputTokens { get; private set; }

    public int EstimatedOutputTokens { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public ICollection<TenantGenerationResponseSource> Sources { get; private set; } = [];

    private TenantGenerationResponse()
    {
    }

    public TenantGenerationResponse(
        int tenantId,
        string promptKey,
        string promptName,
        string input,
        string systemPrompt,
        string userPrompt,
        string finalPrompt,
        string responseText,
        string generationModel,
        GenerationResponseStatus status,
        string? errorMessage,
        string? requestedByUserId,
        string? requestedByDisplayName,
        int estimatedInputTokens,
        int estimatedOutputTokens,
        DateTime now)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantId), "Tenant id must be greater than zero.");

        if (estimatedInputTokens < 0)
            throw new ArgumentOutOfRangeException(nameof(estimatedInputTokens), "Estimated input token count cannot be negative.");

        if (estimatedOutputTokens < 0)
            throw new ArgumentOutOfRangeException(nameof(estimatedOutputTokens), "Estimated output token count cannot be negative.");

        ResponseKey = Guid.NewGuid();
        TenantId = tenantId;
        PromptKey = NormalizeRequired(promptKey, MaxPromptKeyLength, nameof(PromptKey));
        PromptName = NormalizeRequired(promptName, MaxPromptNameLength, nameof(PromptName));
        Input = NormalizeRequired(input, int.MaxValue, nameof(Input));
        SystemPrompt = NormalizeRequired(systemPrompt, int.MaxValue, nameof(SystemPrompt));
        UserPrompt = NormalizeRequired(userPrompt, int.MaxValue, nameof(UserPrompt));
        FinalPrompt = NormalizeRequired(finalPrompt, int.MaxValue, nameof(FinalPrompt));
        ResponseText = status == GenerationResponseStatus.Completed
            ? NormalizeRequired(responseText, int.MaxValue, nameof(ResponseText))
            : NormalizeOptional(responseText, int.MaxValue, nameof(ResponseText)) ?? string.Empty;
        GenerationModel = NormalizeRequired(generationModel, MaxGenerationModelLength, nameof(GenerationModel));
        Status = status;
        ErrorMessage = NormalizeOptional(errorMessage, MaxErrorMessageLength, nameof(ErrorMessage));
        RequestedByUserId = NormalizeOptional(requestedByUserId, MaxRequestedByUserIdLength, nameof(RequestedByUserId));
        RequestedByDisplayName = NormalizeOptional(requestedByDisplayName, MaxRequestedByDisplayNameLength, nameof(RequestedByDisplayName));
        EstimatedInputTokens = estimatedInputTokens;
        EstimatedOutputTokens = estimatedOutputTokens;
        CreatedAtUtc = now;
        CompletedAtUtc = status == GenerationResponseStatus.Completed ? now : null;
    }

    public void AddSources(IEnumerable<TenantGenerationResponseSource> sources)
    {
        Sources.Clear();

        foreach (var source in sources ?? [])
        {
            Sources.Add(source);
        }

        SourceCount = Sources.Count;
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

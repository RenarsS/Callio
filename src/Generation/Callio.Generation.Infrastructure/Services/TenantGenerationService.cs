using Callio.Generation.Application.Generation;
using Callio.Generation.Domain;
using Callio.Generation.Domain.Enums;
using Callio.Generation.Infrastructure.Options;
using Callio.Knowledge.Application.KnowledgeConfigurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace Callio.Generation.Infrastructure.Services;

public class TenantGenerationService(
    IGenerationPromptCatalog promptCatalog,
    ITenantKnowledgeConfigurationService knowledgeConfigurationService,
    IGenerationKnowledgeSourceProvider sourceProvider,
    IGenerationCompletionClient completionClient,
    ITenantGenerationRepository repository,
    IOptions<TenantGenerationOptions> options,
    ILogger<TenantGenerationService> logger) : ITenantGenerationService
{
    private readonly TenantGenerationOptions _options = options.Value;

    public Task<IReadOnlyList<GenerationPromptTemplateDto>> GetPromptTemplatesAsync(CancellationToken cancellationToken = default)
        => promptCatalog.GetTemplatesAsync(cancellationToken);

    public async Task<GenerationResponseDto> GenerateAsync(
        GenerateTenantResponseCommand command,
        CancellationToken cancellationToken = default)
    {
        ValidateCommand(command);

        var configuration = await GetOrCreateConfigurationAsync(command.TenantId, cancellationToken);
        var template = await promptCatalog.GetTemplateAsync(command.PromptKey, cancellationToken);
        var dataSources = MergeDataSources(template.DataSources, command.DataSources);
        var sources = await sourceProvider.RetrieveAsync(
            command.TenantId,
            command.Input,
            configuration,
            dataSources,
            cancellationToken);

        var composition = ComposePrompt(command, configuration, template, sources);
        var model = string.IsNullOrWhiteSpace(configuration.Models.GenerationModel)
            ? _options.GenerationModel
            : configuration.Models.GenerationModel;

        TenantGenerationResponse response;
        try
        {
            var completion = await completionClient.CompleteAsync(
                composition.SystemPrompt,
                composition.UserPrompt,
                model,
                cancellationToken);

            response = CreateResponse(
                command,
                template,
                composition,
                completion.ResponseText,
                completion.Model,
                GenerationResponseStatus.Completed,
                errorMessage: null,
                completion.EstimatedOutputTokens);
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
        {
            logger.LogWarning(ex, "Tenant generation failed for tenant {TenantId}.", command.TenantId);
            response = CreateResponse(
                command,
                template,
                composition,
                responseText: string.Empty,
                generationModel: model,
                GenerationResponseStatus.Failed,
                ex.GetBaseException().Message,
                estimatedOutputTokens: 0);
        }

        if (!command.SaveResponse)
            return response.ToDto();

        var saved = await repository.AddAsync(response, cancellationToken);
        return saved.ToDto();
    }

    public async Task<IReadOnlyList<GenerationResponseDto>> GetResponsesAsync(
        int tenantId,
        GetTenantGenerationResponsesQuery query,
        CancellationToken cancellationToken = default)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantId), "Tenant id must be greater than zero.");

        var take = query.Take.GetValueOrDefault(25);
        var responses = await repository.GetRecentAsync(tenantId, take, cancellationToken);
        return responses.Select(x => x.ToDto()).ToList();
    }

    public async Task<GenerationResponseDto?> GetResponseAsync(
        int tenantId,
        int responseId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId <= 0 || responseId <= 0)
            throw new ArgumentOutOfRangeException(nameof(responseId), "Tenant id and response id must be greater than zero.");

        var response = await repository.GetByIdAsync(tenantId, responseId, cancellationToken);
        return response?.ToDto();
    }

    private async Task<TenantKnowledgeConfigurationDto> GetOrCreateConfigurationAsync(
        int tenantId,
        CancellationToken cancellationToken)
    {
        var active = await knowledgeConfigurationService.GetActiveAsync(tenantId, cancellationToken);
        if (active is not null)
            return active;

        return await knowledgeConfigurationService.CreateDefaultAsync(
            new CreateDefaultTenantKnowledgeConfigurationCommand(tenantId),
            cancellationToken);
    }

    private GenerationPromptCompositionDto ComposePrompt(
        GenerateTenantResponseCommand command,
        TenantKnowledgeConfigurationDto configuration,
        GenerationPromptTemplateDto template,
        IReadOnlyList<RetrievedGenerationSourceDto> sources)
    {
        var context = CreateContextBlock(sources);
        var replacements = CreateReplacements(command, configuration, context, sources.Count);
        var systemTemplate = string.IsNullOrWhiteSpace(template.SystemPrompt)
            ? configuration.SystemPrompt
            : template.SystemPrompt;
        var userTemplate = string.IsNullOrWhiteSpace(template.UserPromptTemplate)
            ? "{{input}}\n\n{{context}}"
            : template.UserPromptTemplate;

        var systemPrompt = ApplyReplacements(systemTemplate, replacements);
        var userPrompt = ApplyReplacements(userTemplate, replacements);

        if (!ContainsToken(userTemplate, "context") && !ContainsToken(userTemplate, "sources"))
            userPrompt = $"{userPrompt.Trim()}\n\nRetrieved context:\n{context}";

        var finalPrompt = $"System:\n{systemPrompt.Trim()}\n\nUser:\n{userPrompt.Trim()}";
        return new GenerationPromptCompositionDto(
            systemPrompt,
            userPrompt,
            finalPrompt,
            sources,
            EstimateTokens(finalPrompt));
    }

    private static IReadOnlyDictionary<string, string> CreateReplacements(
        GenerateTenantResponseCommand command,
        TenantKnowledgeConfigurationDto configuration,
        string context,
        int sourceCount)
    {
        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["input"] = command.Input,
            ["question"] = command.Input,
            ["context"] = context,
            ["sources"] = context,
            ["assistantInstructionPrompt"] = configuration.AssistantInstructionPrompt,
            ["knowledgeSystemPrompt"] = configuration.SystemPrompt,
            ["generationModel"] = configuration.Models.GenerationModel,
            ["sourceCount"] = sourceCount.ToString()
        };

        foreach (var variable in command.Variables)
        {
            if (!string.IsNullOrWhiteSpace(variable.Key))
                replacements[variable.Key.Trim()] = variable.Value ?? string.Empty;
        }

        return replacements;
    }

    private static string ApplyReplacements(
        string template,
        IReadOnlyDictionary<string, string> replacements)
    {
        var result = template ?? string.Empty;
        foreach (var replacement in replacements)
        {
            result = result.Replace(
                "{{" + replacement.Key + "}}",
                replacement.Value,
                StringComparison.OrdinalIgnoreCase);
        }

        return result.Trim();
    }

    private static bool ContainsToken(string template, string token)
        => (template ?? string.Empty).Contains("{{" + token + "}}", StringComparison.OrdinalIgnoreCase);

    private static string CreateContextBlock(IReadOnlyList<RetrievedGenerationSourceDto> sources)
    {
        if (sources.Count == 0)
            return "No matching approved tenant knowledge context was retrieved.";

        var builder = new StringBuilder();
        for (var i = 0; i < sources.Count; i++)
        {
            var source = sources[i];
            var labelParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(source.DocumentTitle))
                labelParts.Add(source.DocumentTitle);
            if (!string.IsNullOrWhiteSpace(source.CategoryName))
                labelParts.Add($"category: {source.CategoryName}");
            if (source.ChunkIndex.HasValue)
                labelParts.Add($"chunk: {source.ChunkIndex.Value}");
            if (source.Score.HasValue)
                labelParts.Add($"score: {source.Score.Value:0.000000}");
            if (!string.IsNullOrWhiteSpace(source.BlobUri))
                labelParts.Add($"blob: {source.BlobUri}");

            builder.Append('[').Append(i + 1).Append("] ");
            builder.AppendLine(labelParts.Count == 0 ? source.SourceKind : string.Join("; ", labelParts));
            builder.AppendLine(source.Content);
            builder.AppendLine();
        }

        return builder.ToString().Trim();
    }

    private static IReadOnlyList<GenerationDataSourceSelectionDto> MergeDataSources(
        IReadOnlyList<GenerationDataSourceSelectionDto> templateDataSources,
        IReadOnlyList<GenerationDataSourceSelectionDto> commandDataSources)
    {
        var merged = templateDataSources
            .Concat(commandDataSources)
            .Where(x => x is not null)
            .ToList();

        return merged.Count > 0
            ? merged
            : [new GenerationDataSourceSelectionDto("KnowledgeChunk", null, null, null, null, null, null, false)];
    }

    private TenantGenerationResponse CreateResponse(
        GenerateTenantResponseCommand command,
        GenerationPromptTemplateDto template,
        GenerationPromptCompositionDto composition,
        string responseText,
        string generationModel,
        GenerationResponseStatus status,
        string? errorMessage,
        int estimatedOutputTokens)
    {
        var now = DateTime.UtcNow;
        var response = new TenantGenerationResponse(
            command.TenantId,
            template.Key,
            template.Name,
            command.Input,
            composition.SystemPrompt,
            composition.UserPrompt,
            composition.FinalPrompt,
            responseText,
            generationModel,
            status,
            errorMessage,
            command.RequestedByUserId,
            command.RequestedByDisplayName,
            composition.EstimatedInputTokens,
            estimatedOutputTokens,
            now);

        response.AddSources(composition.Sources.Select(source => new TenantGenerationResponseSource(
            ParseSourceKind(source.SourceKind),
            source.KnowledgeDocumentId,
            source.DocumentTitle,
            source.CategoryId,
            source.CategoryName,
            source.ChunkId,
            source.ChunkIndex,
            source.Score,
            source.BlobContainerName,
            source.BlobName,
            source.BlobUri,
            source.Content,
            now)));

        return response;
    }

    private static GenerationSourceKind ParseSourceKind(string sourceKind)
        => Enum.TryParse<GenerationSourceKind>(sourceKind, ignoreCase: true, out var parsed)
            ? parsed
            : GenerationSourceKind.KnowledgeChunk;

    private static void ValidateCommand(GenerateTenantResponseCommand command)
    {
        if (command.TenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(command.TenantId), "Tenant id must be greater than zero.");

        if (string.IsNullOrWhiteSpace(command.Input))
            throw new ArgumentException("Generation input is required.", nameof(command.Input));
    }

    private static int EstimateTokens(string value)
        => Math.Max(1, (int)Math.Ceiling((value?.Length ?? 0) / 4d));
}

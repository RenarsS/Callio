using Callio.Generation.Application.Generation;
using Callio.Generation.Domain;
using Callio.Generation.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Callio.Generation.Infrastructure.Services;

public class GenerationPromptCatalog(
    ITenantGenerationRepository repository,
    IOptions<TenantGenerationOptions> options) : IGenerationPromptCatalog
{
    private readonly TenantGenerationOptions _options = options.Value;

    public async Task<IReadOnlyList<GenerationPromptTemplateDto>> GetTemplatesAsync(
        int tenantId,
        CancellationToken cancellationToken = default)
        => (await EnsureTemplatesAsync(tenantId, cancellationToken))
            .Select(x => x.ToDto())
            .ToList();

    public async Task<GenerationPromptTemplateDto> GetTemplateAsync(
        int tenantId,
        string? promptKey,
        CancellationToken cancellationToken = default)
    {
        var templates = await EnsureTemplatesAsync(tenantId, cancellationToken);
        var key = string.IsNullOrWhiteSpace(promptKey)
            ? _options.DefaultPromptKey
            : promptKey.Trim();

        var template = templates.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase))
                       ?? templates.FirstOrDefault(x => string.Equals(x.Key, _options.DefaultPromptKey, StringComparison.OrdinalIgnoreCase))
                       ?? templates.First();

        return template.ToDto();
    }

    public async Task<GenerationPromptTemplateDto> CreateTemplateAsync(
        CreateTenantGenerationPromptTemplateCommand command,
        CancellationToken cancellationToken = default)
    {
        await EnsureTemplatesAsync(command.TenantId, cancellationToken);

        var existing = await repository.GetPromptTemplateByKeyAsync(command.TenantId, command.Key.Trim(), cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"A generation prompt with key '{command.Key.Trim()}' already exists for this tenant.");

        var promptTemplate = new TenantGenerationPromptTemplate(
            command.TenantId,
            command.Key,
            command.Name,
            command.Description,
            command.SystemPrompt,
            command.UserPromptTemplate,
            TenantGenerationMappings.SerializeDataSources(NormalizeDataSources(command.DataSources)),
            DateTime.UtcNow);

        return (await repository.AddPromptTemplateAsync(promptTemplate, cancellationToken)).ToDto();
    }

    public async Task<GenerationPromptTemplateDto?> UpdateTemplateAsync(
        UpdateTenantGenerationPromptTemplateCommand command,
        CancellationToken cancellationToken = default)
    {
        await EnsureTemplatesAsync(command.TenantId, cancellationToken);

        var promptTemplate = await repository.GetPromptTemplateByIdAsync(command.TenantId, command.PromptTemplateId, cancellationToken);
        if (promptTemplate is null)
            return null;

        var existing = await repository.GetPromptTemplateByKeyAsync(command.TenantId, command.Key.Trim(), cancellationToken);
        if (existing is not null && existing.Id != promptTemplate.Id)
            throw new InvalidOperationException($"A generation prompt with key '{command.Key.Trim()}' already exists for this tenant.");

        promptTemplate.Update(
            command.Key,
            command.Name,
            command.Description,
            command.SystemPrompt,
            command.UserPromptTemplate,
            TenantGenerationMappings.SerializeDataSources(NormalizeDataSources(command.DataSources)),
            DateTime.UtcNow);

        return (await repository.UpdatePromptTemplateAsync(promptTemplate, cancellationToken)).ToDto();
    }

    private async Task<IReadOnlyList<TenantGenerationPromptTemplate>> EnsureTemplatesAsync(
        int tenantId,
        CancellationToken cancellationToken)
    {
        var existing = await repository.GetPromptTemplatesAsync(tenantId, cancellationToken);
        if (existing.Count > 0)
            return existing;

        var now = DateTime.UtcNow;
        foreach (var configuredTemplate in GetConfiguredTemplates())
        {
            await repository.AddPromptTemplateAsync(
                new TenantGenerationPromptTemplate(
                    tenantId,
                    configuredTemplate.Key,
                    configuredTemplate.Name,
                    configuredTemplate.Description,
                    configuredTemplate.SystemPrompt,
                    configuredTemplate.UserPromptTemplate,
                    TenantGenerationMappings.SerializeDataSources(configuredTemplate.DataSources),
                    now),
                cancellationToken);
        }

        return await repository.GetPromptTemplatesAsync(tenantId, cancellationToken);
    }

    private IReadOnlyList<ConfiguredPromptTemplate> GetConfiguredTemplates()
    {
        var configured = _options.PromptTemplates?
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .Select(x => new ConfiguredPromptTemplate(
                x.Key.Trim(),
                string.IsNullOrWhiteSpace(x.Name) ? x.Key.Trim() : x.Name.Trim(),
                string.IsNullOrWhiteSpace(x.Description) ? null : x.Description.Trim(),
                x.SystemPrompt,
                x.UserPromptTemplate,
                NormalizeDataSources(x.DataSources.Select(MapDataSource).ToList())))
            .ToList();

        return configured is { Count: > 0 }
            ? configured
            : new TenantGenerationOptions().PromptTemplates.Select(x => new ConfiguredPromptTemplate(
                x.Key,
                x.Name,
                x.Description,
                x.SystemPrompt,
                x.UserPromptTemplate,
                NormalizeDataSources(x.DataSources.Select(MapDataSource).ToList()))).ToList();
    }

    private static IReadOnlyList<GenerationDataSourceSelectionDto> NormalizeDataSources(
        IReadOnlyList<GenerationDataSourceSelectionDto>? dataSources)
        => dataSources is { Count: > 0 }
            ? dataSources
            : [new GenerationDataSourceSelectionDto("KnowledgeChunk", null, null, null, null, null, null, false)];

    private static GenerationDataSourceSelectionDto MapDataSource(TenantGenerationDataSourceOptions source)
        => new(
            source.SourceKind,
            source.CategoryId,
            source.CategoryName,
            source.TagId,
            source.TagName,
            source.DocumentId,
            source.MaxChunks,
            source.IncludeBlobContent);

    private sealed record ConfiguredPromptTemplate(
        string Key,
        string Name,
        string? Description,
        string SystemPrompt,
        string UserPromptTemplate,
        IReadOnlyList<GenerationDataSourceSelectionDto> DataSources);
}

using Callio.Generation.Application.Generation;
using Callio.Generation.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Callio.Generation.Infrastructure.Services;

public class GenerationPromptCatalog(IOptions<TenantGenerationOptions> options) : IGenerationPromptCatalog
{
    private readonly TenantGenerationOptions _options = options.Value;

    public Task<IReadOnlyList<GenerationPromptTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<GenerationPromptTemplateDto>>(GetTemplates());

    public Task<GenerationPromptTemplateDto> GetTemplateAsync(string? promptKey, CancellationToken cancellationToken = default)
    {
        var templates = GetTemplates();
        var key = string.IsNullOrWhiteSpace(promptKey)
            ? _options.DefaultPromptKey
            : promptKey.Trim();

        var template = templates.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase))
                       ?? templates.FirstOrDefault(x => string.Equals(x.Key, _options.DefaultPromptKey, StringComparison.OrdinalIgnoreCase))
                       ?? templates.First();

        return Task.FromResult(template);
    }

    private IReadOnlyList<GenerationPromptTemplateDto> GetTemplates()
    {
        var configured = _options.PromptTemplates?
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .Select(x => new GenerationPromptTemplateDto(
                x.Key.Trim(),
                string.IsNullOrWhiteSpace(x.Name) ? x.Key.Trim() : x.Name.Trim(),
                string.IsNullOrWhiteSpace(x.Description) ? null : x.Description.Trim(),
                x.SystemPrompt,
                x.UserPromptTemplate,
                x.DataSources.Select(MapDataSource).ToList()))
            .ToList();

        return configured is { Count: > 0 }
            ? configured
            : new TenantGenerationOptions().PromptTemplates.Select(x => new GenerationPromptTemplateDto(
                x.Key,
                x.Name,
                x.Description,
                x.SystemPrompt,
                x.UserPromptTemplate,
                x.DataSources.Select(MapDataSource).ToList())).ToList();
    }

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
}

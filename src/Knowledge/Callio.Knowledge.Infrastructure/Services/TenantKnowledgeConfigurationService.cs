using Callio.Knowledge.Application.KnowledgeConfigurations;
using Callio.Knowledge.Domain;
using Callio.Knowledge.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Callio.Knowledge.Infrastructure.Services;

public class TenantKnowledgeConfigurationService(
    ITenantKnowledgeConfigurationRepository repository,
    IOptions<TenantKnowledgeConfigurationOptions> options) : ITenantKnowledgeConfigurationService
{
    private readonly TenantKnowledgeConfigurationOptions _options = options.Value;

    public async Task<TenantKnowledgeConfigurationDto> CreateDefaultAsync(
        CreateDefaultTenantKnowledgeConfigurationCommand command,
        CancellationToken cancellationToken = default)
    {
        var active = await repository.GetActiveAsync(command.TenantId, cancellationToken);
        if (active is not null)
            return active.ToDto(CreateModelConstraints());

        var configuration = new TenantKnowledgeConfiguration(
            command.TenantId,
            _options.DefaultSystemPrompt,
            _options.DefaultAssistantInstructionPrompt,
            _options.ChunkSize,
            _options.ChunkOverlap,
            _options.RetrievalTopK,
            _options.MaximumChunksInFinalContext,
            _options.MinimumSimilarityThreshold,
            _options.AllowedFileTypes,
            _options.MaximumFileSizeBytes,
            _options.AutoProcessOnUpload,
            _options.ManualApprovalRequiredBeforeIndexing,
            _options.VersioningEnabled,
            isActive: true,
            DateTime.UtcNow);

        await repository.AddAsync(configuration, cancellationToken);
        return configuration.ToDto(CreateModelConstraints());
    }

    public async Task<TenantKnowledgeConfigurationDto?> GetActiveAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var configuration = await repository.GetActiveAsync(tenantId, cancellationToken);
        return configuration?.ToDto(CreateModelConstraints());
    }

    public async Task<TenantKnowledgeConfigurationDto?> GetByIdAsync(int tenantId, int configurationId, CancellationToken cancellationToken = default)
    {
        var configuration = await repository.GetByIdAsync(tenantId, configurationId, cancellationToken);
        return configuration?.ToDto(CreateModelConstraints());
    }

    public async Task<TenantKnowledgeConfigurationDto?> UpdateAsync(
        UpdateTenantKnowledgeConfigurationCommand command,
        CancellationToken cancellationToken = default)
    {
        var configuration = await repository.GetByIdAsync(command.TenantId, command.ConfigurationId, cancellationToken);
        if (configuration is null)
            return null;

        configuration.Update(
            command.SystemPrompt,
            command.AssistantInstructionPrompt,
            command.ChunkSize,
            command.ChunkOverlap,
            command.TopKRetrievalCount,
            command.MaximumChunksInFinalContext,
            command.MinimumSimilarityThreshold,
            command.AllowedFileTypes,
            command.MaximumFileSizeBytes,
            command.AutoProcessOnUpload,
            command.ManualApprovalRequiredBeforeIndexing,
            command.VersioningEnabled,
            DateTime.UtcNow);

        await repository.UpdateAsync(configuration, cancellationToken);
        return configuration.ToDto(CreateModelConstraints());
    }

    public async Task<TenantKnowledgeConfigurationDto?> ActivateAsync(
        ChangeTenantKnowledgeConfigurationStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var configuration = await repository.ActivateAsync(
            command.TenantId,
            command.ConfigurationId,
            DateTime.UtcNow,
            cancellationToken);

        return configuration?.ToDto(CreateModelConstraints());
    }

    public async Task<TenantKnowledgeConfigurationDto?> DeactivateAsync(
        ChangeTenantKnowledgeConfigurationStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var configuration = await repository.DeactivateAsync(
            command.TenantId,
            command.ConfigurationId,
            DateTime.UtcNow,
            cancellationToken);

        return configuration?.ToDto(CreateModelConstraints());
    }

    private KnowledgeModelConstraintsDto CreateModelConstraints()
        => new(
            _options.EmbeddingProvider,
            _options.EmbeddingModel,
            _options.GenerationModel);
}

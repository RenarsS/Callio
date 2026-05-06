using Callio.Core.Infrastructure.Messaging.Knowledge;
using Callio.Generation.Application.Generation;
using MassTransit;

namespace Callio.Generation.Infrastructure.Services;

public class BrokeredGenerationKnowledgeSourceProvider(
    IRequestClient<RetrieveTenantGenerationSourcesRequest> requestClient) : IGenerationKnowledgeSourceProvider
{
    public async Task<TenantGenerationKnowledgeContextDto> RetrieveAsync(
        int tenantId,
        string input,
        IReadOnlyList<GenerationDataSourceSelectionDto> dataSources,
        CancellationToken cancellationToken = default)
    {
        var response = await requestClient.GetResponse<RetrieveTenantGenerationSourcesResponse>(
            new RetrieveTenantGenerationSourcesRequest(
                tenantId,
                input,
                dataSources.Select(MapDataSource).ToList()),
            cancellationToken);

        return new TenantGenerationKnowledgeContextDto(
            MapConfiguration(response.Message.Configuration),
            response.Message.Sources.Select(MapSource).ToList());
    }

    private static TenantGenerationDataSourceSelectionMessage MapDataSource(GenerationDataSourceSelectionDto source)
        => new(
            source.SourceKind,
            source.CategoryId,
            source.CategoryName,
            source.TagId,
            source.TagName,
            source.DocumentId,
            source.MaxChunks,
            source.IncludeBlobContent);

    private static TenantGenerationKnowledgeConfigurationDto MapConfiguration(TenantKnowledgeConfigurationMessage configuration)
        => new(
            configuration.Id,
            configuration.TenantId,
            configuration.SystemPrompt,
            configuration.AssistantInstructionPrompt,
            configuration.ChunkSize,
            configuration.ChunkOverlap,
            configuration.TopKRetrievalCount,
            configuration.MaximumChunksInFinalContext,
            configuration.MinimumSimilarityThreshold,
            configuration.AllowedFileTypes,
            configuration.MaximumFileSizeBytes,
            configuration.AutoProcessOnUpload,
            configuration.ManualApprovalRequiredBeforeIndexing,
            configuration.VersioningEnabled,
            configuration.IsActive,
            configuration.CreatedAtUtc,
            configuration.UpdatedAtUtc,
            new TenantGenerationKnowledgeModelConstraintsDto(
                configuration.Models.EmbeddingProvider,
                configuration.Models.EmbeddingModel,
                configuration.Models.GenerationModel));

    private static RetrievedGenerationSourceDto MapSource(RetrievedTenantGenerationSourceMessage source)
        => new(
            source.SourceKind,
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
            source.Content);
}

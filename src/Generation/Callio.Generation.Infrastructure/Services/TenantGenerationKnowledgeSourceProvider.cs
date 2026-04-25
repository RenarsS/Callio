using Callio.Generation.Application.Generation;
using Callio.Generation.Infrastructure.Options;
using Callio.Knowledge.Application.KnowledgeConfigurations;
using Callio.Knowledge.Domain;
using Callio.Knowledge.Domain.Enums;
using Callio.Knowledge.Infrastructure.Persistence;
using Callio.Knowledge.Infrastructure.Provisioners;
using Callio.Knowledge.Infrastructure.Services.KnowledgeDocuments;
using Callio.Provisioning.Infrastructure.Persistence;
using Callio.Provisioning.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Callio.Generation.Infrastructure.Services;

public class TenantGenerationKnowledgeSourceProvider(
    ProvisioningDbContext provisioningDbContext,
    ITenantResourceNamingStrategy tenantResourceNamingStrategy,
    ITenantKnowledgeDocumentDbContextFactory dbContextFactory,
    ITenantKnowledgeDocumentStoreProvisioner storeProvisioner,
    ITenantEmbeddingGenerator embeddingGenerator,
    ITenantKnowledgeBlobStorage blobStorage,
    ITenantKnowledgeTextExtractor textExtractor,
    IOptions<TenantGenerationOptions> options,
    ILogger<TenantGenerationKnowledgeSourceProvider> logger) : IGenerationKnowledgeSourceProvider
{
    private readonly TenantGenerationOptions _options = options.Value;

    public async Task<IReadOnlyList<RetrievedGenerationSourceDto>> RetrieveAsync(
        int tenantId,
        string input,
        TenantKnowledgeConfigurationDto configuration,
        IReadOnlyList<GenerationDataSourceSelectionDto> dataSources,
        CancellationToken cancellationToken = default)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantId), "Tenant id must be greater than zero.");

        var selections = NormalizeSelections(dataSources);
        var schemaName = await ResolveSchemaNameAsync(tenantId, cancellationToken);
        await storeProvisioner.EnsureCreatedAsync(schemaName, cancellationToken);

        await using var context = dbContextFactory.Create(schemaName);

        var categoryIds = await ResolveCategoryIdsAsync(context, tenantId, selections, cancellationToken);
        var tagIds = await ResolveTagIdsAsync(context, tenantId, selections, cancellationToken);
        var documentIds = selections
            .Where(x => x.DocumentId.HasValue && x.DocumentId.Value > 0)
            .Select(x => x.DocumentId!.Value)
            .Distinct()
            .ToList();

        var documents = await LoadDocumentsAsync(
            context,
            tenantId,
            categoryIds,
            tagIds,
            documentIds,
            cancellationToken);

        if (documents.Count == 0)
            return [];

        var chunkSources = await BuildChunkSourcesAsync(
            input,
            configuration,
            selections,
            documents,
            cancellationToken);

        var sources = chunkSources.ToList();
        if (ShouldIncludeBlobContent(selections))
        {
            var blobSources = await BuildBlobSourcesAsync(
                documents,
                chunkSources,
                cancellationToken);

            sources.AddRange(blobSources);
        }

        return sources
            .OrderByDescending(x => x.Score ?? 0)
            .ThenBy(x => x.KnowledgeDocumentId)
            .ThenBy(x => x.ChunkIndex)
            .Take(ResolveFinalLimit(configuration, selections) + ResolveBlobLimit(selections))
            .ToList();
    }

    private async Task<IReadOnlyList<RetrievedGenerationSourceDto>> BuildChunkSourcesAsync(
        string input,
        TenantKnowledgeConfigurationDto configuration,
        IReadOnlyList<GenerationDataSourceSelectionDto> selections,
        IReadOnlyList<TenantKnowledgeDocument> documents,
        CancellationToken cancellationToken)
    {
        var chunks = documents
            .SelectMany(document => document.Chunks.Select(chunk => (Document: document, Chunk: chunk)))
            .ToList();

        if (chunks.Count == 0)
            return [];

        var queryEmbedding = await GenerateQueryEmbeddingAsync(input, configuration.Models.EmbeddingModel, cancellationToken);
        var scored = chunks
            .Select(item => CreateScoredChunk(item.Document, item.Chunk, queryEmbedding))
            .Where(item => item.Score is null || item.Score >= configuration.MinimumSimilarityThreshold)
            .OrderByDescending(item => item.Score ?? 0)
            .ThenBy(item => item.Document.Id)
            .ThenBy(item => item.Chunk.ChunkIndex)
            .Take(ResolveFinalLimit(configuration, selections))
            .Select(item => new RetrievedGenerationSourceDto(
                "KnowledgeChunk",
                item.Document.Id,
                item.Document.Title,
                item.Document.CategoryId,
                item.Document.Category?.Name,
                item.Chunk.Id,
                item.Chunk.ChunkIndex,
                item.Score,
                item.Document.BlobContainerName,
                item.Document.BlobName,
                item.Document.BlobUri,
                LimitContent(item.Chunk.Content, _options.SourceExcerptMaxCharacters)))
            .ToList();

        return scored;
    }

    private async Task<IReadOnlyList<RetrievedGenerationSourceDto>> BuildBlobSourcesAsync(
        IReadOnlyList<TenantKnowledgeDocument> documents,
        IReadOnlyList<RetrievedGenerationSourceDto> chunkSources,
        CancellationToken cancellationToken)
    {
        var selectedDocumentIds = chunkSources
            .Select(x => x.KnowledgeDocumentId)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToHashSet();

        var selectedDocuments = selectedDocumentIds.Count > 0
            ? documents.Where(x => selectedDocumentIds.Contains(x.Id)).ToList()
            : documents.Take(3).ToList();

        var results = new List<RetrievedGenerationSourceDto>();
        foreach (var document in selectedDocuments)
        {
            try
            {
                var blob = await blobStorage.DownloadAsync(
                    document.BlobContainerName,
                    document.BlobName,
                    cancellationToken);

                var text = await textExtractor.ExtractTextAsync(
                    document.OriginalFileName,
                    blob.Content,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(text))
                    continue;

                results.Add(new RetrievedGenerationSourceDto(
                    "BlobDocument",
                    document.Id,
                    document.Title,
                    document.CategoryId,
                    document.Category?.Name,
                    null,
                    null,
                    null,
                    document.BlobContainerName,
                    document.BlobName,
                    document.BlobUri,
                    LimitContent(text, _options.BlobContentMaxCharacters)));
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or NotSupportedException)
            {
                logger.LogWarning(
                    ex,
                    "Generation could not read blob content for tenant knowledge document {KnowledgeDocumentId}.",
                    document.Id);
            }
        }

        return results;
    }

    private async Task<IReadOnlyList<TenantKnowledgeDocument>> LoadDocumentsAsync(
        TenantKnowledgeDocumentDbContext context,
        int tenantId,
        IReadOnlyCollection<int> categoryIds,
        IReadOnlyCollection<int> tagIds,
        IReadOnlyCollection<int> documentIds,
        CancellationToken cancellationToken)
    {
        var query = context.Documents
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.DocumentTags)
                .ThenInclude(x => x.Tag)
            .Include(x => x.Chunks)
            .Where(x => x.TenantId == tenantId && x.ProcessingStatus == KnowledgeDocumentProcessingStatus.Ready);

        if (categoryIds.Count > 0 || tagIds.Count > 0 || documentIds.Count > 0)
        {
            query = query.Where(x =>
                documentIds.Contains(x.Id) ||
                (x.CategoryId.HasValue && categoryIds.Contains(x.CategoryId.Value)) ||
                x.DocumentTags.Any(tag => tagIds.Contains(tag.TenantKnowledgeTagId)));
        }

        return await query
            .OrderByDescending(x => x.IndexedAtUtc ?? x.UpdatedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    private static (TenantKnowledgeDocument Document, TenantKnowledgeDocumentChunk Chunk, decimal? Score) CreateScoredChunk(
        TenantKnowledgeDocument document,
        TenantKnowledgeDocumentChunk chunk,
        float[]? queryEmbedding)
    {
        if (queryEmbedding is null)
            return (document, chunk, null);

        var chunkEmbedding = ParseEmbedding(chunk.EmbeddingJson);
        if (chunkEmbedding is null || chunkEmbedding.Length != queryEmbedding.Length)
            return (document, chunk, null);

        var score = CosineSimilarity(queryEmbedding, chunkEmbedding);
        return (document, chunk, (decimal)Math.Round(score, 6));
    }

    private async Task<float[]?> GenerateQueryEmbeddingAsync(
        string input,
        string embeddingModel,
        CancellationToken cancellationToken)
    {
        try
        {
            var embeddings = await embeddingGenerator.GenerateEmbeddingsAsync([input], embeddingModel, cancellationToken);
            return embeddings.FirstOrDefault();
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
        {
            logger.LogWarning(ex, "Generation retrieval could not create a query embedding. Falling back to document order.");
            return null;
        }
    }

    private static float[]? ParseEmbedding(string embeddingJson)
    {
        try
        {
            return JsonSerializer.Deserialize<float[]>(embeddingJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static double CosineSimilarity(float[] left, float[] right)
    {
        double dot = 0;
        double leftMagnitude = 0;
        double rightMagnitude = 0;

        for (var i = 0; i < left.Length; i++)
        {
            dot += left[i] * right[i];
            leftMagnitude += left[i] * left[i];
            rightMagnitude += right[i] * right[i];
        }

        if (leftMagnitude <= 0 || rightMagnitude <= 0)
            return 0;

        return dot / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
    }

    private async Task<IReadOnlyList<int>> ResolveCategoryIdsAsync(
        TenantKnowledgeDocumentDbContext context,
        int tenantId,
        IReadOnlyList<GenerationDataSourceSelectionDto> selections,
        CancellationToken cancellationToken)
    {
        var ids = selections
            .Where(x => x.CategoryId.HasValue && x.CategoryId.Value > 0)
            .Select(x => x.CategoryId!.Value)
            .ToHashSet();

        var categoryNames = selections
            .Where(x => !string.IsNullOrWhiteSpace(x.CategoryName))
            .Select(x => NormalizeLookup(x.CategoryName!))
            .Distinct()
            .ToList();

        if (categoryNames.Count > 0)
        {
            var matches = await context.Categories
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && categoryNames.Contains(x.NormalizedName))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            foreach (var match in matches)
            {
                ids.Add(match);
            }
        }

        return ids.ToList();
    }

    private async Task<IReadOnlyList<int>> ResolveTagIdsAsync(
        TenantKnowledgeDocumentDbContext context,
        int tenantId,
        IReadOnlyList<GenerationDataSourceSelectionDto> selections,
        CancellationToken cancellationToken)
    {
        var ids = selections
            .Where(x => x.TagId.HasValue && x.TagId.Value > 0)
            .Select(x => x.TagId!.Value)
            .ToHashSet();

        var tagNames = selections
            .Where(x => !string.IsNullOrWhiteSpace(x.TagName))
            .Select(x => NormalizeLookup(x.TagName!))
            .Distinct()
            .ToList();

        if (tagNames.Count > 0)
        {
            var matches = await context.Tags
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && tagNames.Contains(x.NormalizedName))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            foreach (var match in matches)
            {
                ids.Add(match);
            }
        }

        return ids.ToList();
    }

    private async Task<string> ResolveSchemaNameAsync(int tenantId, CancellationToken cancellationToken)
    {
        var schemaName = await provisioningDbContext.TenantInfrastructureProvisionings
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.DatabaseSchema)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(schemaName))
            return schemaName;

        return tenantResourceNamingStrategy.Create(tenantId).DatabaseSchema;
    }

    private static IReadOnlyList<GenerationDataSourceSelectionDto> NormalizeSelections(
        IReadOnlyList<GenerationDataSourceSelectionDto> dataSources)
        => dataSources is { Count: > 0 }
            ? dataSources
            : [new GenerationDataSourceSelectionDto("KnowledgeChunk", null, null, null, null, null, null, false)];

    private static bool ShouldIncludeBlobContent(IReadOnlyList<GenerationDataSourceSelectionDto> selections)
        => selections.Any(x =>
            x.IncludeBlobContent ||
            string.Equals(x.SourceKind, "BlobDocument", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.SourceKind, "BlobStorage", StringComparison.OrdinalIgnoreCase));

    private static int ResolveFinalLimit(
        TenantKnowledgeConfigurationDto configuration,
        IReadOnlyList<GenerationDataSourceSelectionDto> selections)
    {
        var sourceLimit = selections
            .Where(x => x.MaxChunks.HasValue && x.MaxChunks.Value > 0)
            .Sum(x => x.MaxChunks!.Value);

        var configuredLimit = Math.Max(1, configuration.MaximumChunksInFinalContext);
        var retrievalLimit = Math.Max(1, configuration.TopKRetrievalCount);
        var limit = sourceLimit > 0 ? sourceLimit : Math.Min(configuredLimit, retrievalLimit);

        return Math.Clamp(limit, 1, configuredLimit);
    }

    private static int ResolveBlobLimit(IReadOnlyList<GenerationDataSourceSelectionDto> selections)
        => ShouldIncludeBlobContent(selections) ? 3 : 0;

    private static string LimitContent(string value, int maxLength)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length <= maxLength)
            return normalized;

        return normalized[..Math.Max(1, maxLength)].TrimEnd() + "...";
    }

    private static string NormalizeLookup(string value)
        => string.Join(' ', (value ?? string.Empty)
            .Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToUpperInvariant();
}

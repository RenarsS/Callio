using Callio.Core.Infrastructure.Messaging.Knowledge;
using Callio.Knowledge.Application.KnowledgeConfigurations;
using Callio.Knowledge.Domain;
using Callio.Knowledge.Domain.Enums;
using Callio.Knowledge.Infrastructure.Options;
using Callio.Knowledge.Infrastructure.Persistence;
using Callio.Knowledge.Infrastructure.Provisioners;
using Callio.Knowledge.Infrastructure.Services.KnowledgeDocuments;
using Callio.Provisioning.Infrastructure.Persistence;
using Callio.Provisioning.Infrastructure.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Callio.Knowledge.Infrastructure.Consumers;

public class RetrieveTenantGenerationSourcesConsumer(
    ITenantKnowledgeConfigurationService configurationService,
    ProvisioningDbContext provisioningDbContext,
    ITenantResourceNamingStrategy tenantResourceNamingStrategy,
    ITenantKnowledgeDocumentDbContextFactory dbContextFactory,
    ITenantKnowledgeDocumentStoreProvisioner storeProvisioner,
    ITenantEmbeddingGenerator embeddingGenerator,
    ITenantKnowledgeVectorStore vectorStore,
    ITenantKnowledgeBlobStorage blobStorage,
    ITenantKnowledgeTextExtractor textExtractor,
    IOptions<TenantGenerationSourceRetrievalOptions> options,
    ILogger<RetrieveTenantGenerationSourcesConsumer> logger)
    : IConsumer<RetrieveTenantGenerationSourcesRequest>
{
    private readonly TenantGenerationSourceRetrievalOptions _options = options.Value;

    public async Task Consume(ConsumeContext<RetrieveTenantGenerationSourcesRequest> context)
    {
        var message = context.Message;
        if (message.TenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(message.TenantId), "Tenant id must be greater than zero.");

        var configuration = await GetOrCreateConfigurationAsync(message.TenantId, context.CancellationToken);
        var sources = await RetrieveAsync(
            message.TenantId,
            message.Input,
            configuration,
            message.DataSources,
            context.CancellationToken);

        await context.RespondAsync(new RetrieveTenantGenerationSourcesResponse(
            MapConfiguration(configuration),
            sources));
    }

    private async Task<TenantKnowledgeConfigurationDto> GetOrCreateConfigurationAsync(
        int tenantId,
        CancellationToken cancellationToken)
    {
        var active = await configurationService.GetActiveAsync(tenantId, cancellationToken);
        if (active is not null)
            return active;

        return await configurationService.CreateDefaultAsync(
            new CreateDefaultTenantKnowledgeConfigurationCommand(tenantId),
            cancellationToken);
    }

    private async Task<IReadOnlyList<RetrievedTenantGenerationSourceMessage>> RetrieveAsync(
        int tenantId,
        string input,
        TenantKnowledgeConfigurationDto configuration,
        IReadOnlyList<TenantGenerationDataSourceSelectionMessage> dataSources,
        CancellationToken cancellationToken)
    {
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
        var documentKeys = await ResolveDocumentKeysAsync(context, tenantId, documentIds, cancellationToken);
        var vectorStoreNamespace = await ResolveVectorStoreNamespaceAsync(tenantId, cancellationToken);

        IReadOnlyList<TenantKnowledgeDocument> documents;
        IReadOnlyList<RetrievedTenantGenerationSourceMessage> chunkSources;

        var vectorSearchResult = await TryBuildVectorChunkSourcesAsync(
            tenantId,
            input,
            configuration,
            selections,
            categoryIds,
            tagIds,
            documentKeys,
            vectorStoreNamespace,
            context,
            cancellationToken);

        if (vectorSearchResult is not null)
        {
            documents = vectorSearchResult.Documents;
            chunkSources = vectorSearchResult.Sources;
        }
        else
        {
            documents = await LoadDocumentsAsync(
                context,
                tenantId,
                categoryIds,
                tagIds,
                documentIds,
                cancellationToken);

            if (documents.Count == 0)
                return [];

            chunkSources = await BuildChunkSourcesAsync(
                input,
                configuration,
                selections,
                documents,
                cancellationToken);
        }

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

    private async Task<VectorChunkRetrievalResult?> TryBuildVectorChunkSourcesAsync(
        int tenantId,
        string input,
        TenantKnowledgeConfigurationDto configuration,
        IReadOnlyList<TenantGenerationDataSourceSelectionMessage> selections,
        IReadOnlyList<int> categoryIds,
        IReadOnlyList<int> tagIds,
        IReadOnlyList<string> documentKeys,
        string vectorStoreNamespace,
        TenantKnowledgeDocumentDbContext context,
        CancellationToken cancellationToken)
    {
        if (!vectorStore.UsesExternalVectorStore)
            return null;

        var queryEmbedding = await GenerateQueryEmbeddingAsync(input, configuration.Models.EmbeddingModel, cancellationToken);
        if (queryEmbedding is null)
            return null;

        IReadOnlyList<TenantKnowledgeVectorSearchResult> vectorMatches;
        try
        {
            vectorMatches = await vectorStore.SearchAsync(
                vectorStoreNamespace,
                new TenantKnowledgeVectorSearchQuery(
                    queryEmbedding,
                    ResolveFinalLimit(configuration, selections),
                    categoryIds.Select(categoryId => TenantVectorStoreCosmosContext.BuildSectionKey(categoryId)).ToList(),
                    documentKeys,
                    categoryIds,
                    tagIds),
                cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or HttpRequestException or Microsoft.Azure.Cosmos.CosmosException)
        {
            logger.LogWarning(
                ex,
                "Generation retrieval could not query the Azure vector store for tenant {TenantId}. Falling back to SQL chunk scoring.",
                tenantId);
            return null;
        }

        var acceptedMatches = vectorMatches
            .Where(x => x.Score >= configuration.MinimumSimilarityThreshold)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.DocumentKey)
            .ThenBy(x => x.ChunkIndex)
            .ToList();

        if (acceptedMatches.Count == 0)
            return null;

        var documents = await LoadDocumentsByKeysAsync(
            context,
            tenantId,
            acceptedMatches.Select(x => x.DocumentKey).Distinct().ToList(),
            cancellationToken);

        if (documents.Count == 0)
            return null;

        var chunkLookup = documents
            .SelectMany(document => document.Chunks.Select(chunk => (Document: document, Chunk: chunk)))
            .ToDictionary(
                item => (item.Document.DocumentKey, item.Chunk.ChunkIndex),
                item => item);

        var sources = acceptedMatches
            .Select(match =>
            {
                if (!chunkLookup.TryGetValue((match.DocumentKey, match.ChunkIndex), out var item))
                    return null;

                return new RetrievedTenantGenerationSourceMessage(
                    "KnowledgeChunk",
                    item.Document.Id,
                    item.Document.Title,
                    item.Document.CategoryId,
                    item.Document.Category?.Name,
                    item.Chunk.Id,
                    item.Chunk.ChunkIndex,
                    match.Score,
                    item.Document.BlobContainerName,
                    item.Document.BlobName,
                    item.Document.BlobUri,
                    LimitContent(item.Chunk.Content, _options.SourceExcerptMaxCharacters));
            })
            .Where(x => x is not null)
            .Select(x => x!)
            .ToList();

        return sources.Count == 0
            ? null
            : new VectorChunkRetrievalResult(sources, documents);
    }

    private async Task<IReadOnlyList<RetrievedTenantGenerationSourceMessage>> BuildChunkSourcesAsync(
        string input,
        TenantKnowledgeConfigurationDto configuration,
        IReadOnlyList<TenantGenerationDataSourceSelectionMessage> selections,
        IReadOnlyList<TenantKnowledgeDocument> documents,
        CancellationToken cancellationToken)
    {
        var chunks = documents
            .SelectMany(document => document.Chunks.Select(chunk => (Document: document, Chunk: chunk)))
            .ToList();

        if (chunks.Count == 0)
            return [];

        var queryEmbedding = await GenerateQueryEmbeddingAsync(input, configuration.Models.EmbeddingModel, cancellationToken);
        var queryTerms = ExtractQueryTerms(input);
        var scoredChunks = chunks
            .Select(item => CreateScoredChunk(item.Document, item.Chunk, queryEmbedding, queryTerms))
            .OrderByDescending(item => item.Score ?? 0)
            .ThenBy(item => item.Document.Id)
            .ThenBy(item => item.Chunk.ChunkIndex)
            .ToList();

        var limit = ResolveFinalLimit(configuration, selections);
        var selectedChunks = scoredChunks
            .Where(item => item.Score is null || item.Score >= configuration.MinimumSimilarityThreshold)
            .Take(limit)
            .ToList();

        if (selectedChunks.Count == 0)
            selectedChunks = scoredChunks.Take(limit).ToList();

        return selectedChunks
            .Select(item => new RetrievedTenantGenerationSourceMessage(
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
    }

    private async Task<IReadOnlyList<RetrievedTenantGenerationSourceMessage>> BuildBlobSourcesAsync(
        IReadOnlyList<TenantKnowledgeDocument> documents,
        IReadOnlyList<RetrievedTenantGenerationSourceMessage> chunkSources,
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

        var results = new List<RetrievedTenantGenerationSourceMessage>();
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

                results.Add(new RetrievedTenantGenerationSourceMessage(
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

    private static async Task<IReadOnlyList<TenantKnowledgeDocument>> LoadDocumentsByKeysAsync(
        TenantKnowledgeDocumentDbContext context,
        int tenantId,
        IReadOnlyCollection<Guid> documentKeys,
        CancellationToken cancellationToken)
    {
        if (documentKeys.Count == 0)
            return [];

        return await context.Documents
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Chunks)
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProcessingStatus == KnowledgeDocumentProcessingStatus.Ready &&
                documentKeys.Contains(x.DocumentKey))
            .ToListAsync(cancellationToken);
    }

    private static ScoredTenantKnowledgeChunk CreateScoredChunk(
        TenantKnowledgeDocument document,
        TenantKnowledgeDocumentChunk chunk,
        float[]? queryEmbedding,
        IReadOnlyList<string> queryTerms)
    {
        var vectorScore = CalculateVectorScore(chunk, queryEmbedding);
        var keywordScore = CalculateKeywordScore(document, chunk, queryTerms);

        return new ScoredTenantKnowledgeChunk(
            document,
            chunk,
            CombineScores(vectorScore, keywordScore));
    }

    private static decimal? CalculateVectorScore(
        TenantKnowledgeDocumentChunk chunk,
        float[]? queryEmbedding)
    {
        if (queryEmbedding is null)
            return null;

        var chunkEmbedding = ParseEmbedding(chunk.EmbeddingJson);
        if (chunkEmbedding is null || chunkEmbedding.Length != queryEmbedding.Length)
            return null;

        var score = CosineSimilarity(queryEmbedding, chunkEmbedding);
        return (decimal)Math.Round(score, 6);
    }

    private static decimal? CalculateKeywordScore(
        TenantKnowledgeDocument document,
        TenantKnowledgeDocumentChunk chunk,
        IReadOnlyList<string> queryTerms)
    {
        if (queryTerms.Count == 0)
            return null;

        var haystack = $"{document.Title} {document.Category?.Name} {chunk.Content}".ToLowerInvariant();
        var matchedTerms = queryTerms.Count(term => haystack.Contains(term, StringComparison.Ordinal));
        if (matchedTerms == 0)
            return null;

        var coverage = (decimal)matchedTerms / queryTerms.Count;
        var titleBonus = queryTerms.Any(term => document.Title.Contains(term, StringComparison.OrdinalIgnoreCase))
            ? 0.10m
            : 0m;
        var phraseBonus = chunk.Content.Contains(string.Join(' ', queryTerms), StringComparison.OrdinalIgnoreCase)
            ? 0.15m
            : 0m;

        return Math.Round(Math.Min(1m, coverage + titleBonus + phraseBonus), 6);
    }

    private static decimal? CombineScores(decimal? vectorScore, decimal? keywordScore)
    {
        if (vectorScore.HasValue && keywordScore.HasValue)
            return Math.Round((vectorScore.Value * 0.75m) + (keywordScore.Value * 0.25m), 6);

        return vectorScore ?? keywordScore;
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
        IReadOnlyList<TenantGenerationDataSourceSelectionMessage> selections,
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
        IReadOnlyList<TenantGenerationDataSourceSelectionMessage> selections,
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

    private async Task<IReadOnlyList<string>> ResolveDocumentKeysAsync(
        TenantKnowledgeDocumentDbContext context,
        int tenantId,
        IReadOnlyList<int> documentIds,
        CancellationToken cancellationToken)
    {
        if (documentIds.Count == 0)
            return [];

        var keys = await context.Documents
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProcessingStatus == KnowledgeDocumentProcessingStatus.Ready &&
                documentIds.Contains(x.Id))
            .Select(x => x.DocumentKey)
            .ToListAsync(cancellationToken);

        return keys
            .Select(x => x.ToString("N"))
            .ToList();
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

    private async Task<string> ResolveVectorStoreNamespaceAsync(int tenantId, CancellationToken cancellationToken)
    {
        var namespaceName = await provisioningDbContext.TenantInfrastructureProvisionings
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.VectorStoreNamespace)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(namespaceName))
            return namespaceName;

        return tenantResourceNamingStrategy.Create(tenantId).VectorStoreNamespace;
    }

    private static IReadOnlyList<TenantGenerationDataSourceSelectionMessage> NormalizeSelections(
        IReadOnlyList<TenantGenerationDataSourceSelectionMessage> dataSources)
        => dataSources is { Count: > 0 }
            ? dataSources
            : [new TenantGenerationDataSourceSelectionMessage("KnowledgeChunk", null, null, null, null, null, null, false)];

    private static bool ShouldIncludeBlobContent(IReadOnlyList<TenantGenerationDataSourceSelectionMessage> selections)
        => selections.Any(x =>
            x.IncludeBlobContent ||
            string.Equals(x.SourceKind, "BlobDocument", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.SourceKind, "BlobStorage", StringComparison.OrdinalIgnoreCase));

    private static int ResolveFinalLimit(
        TenantKnowledgeConfigurationDto configuration,
        IReadOnlyList<TenantGenerationDataSourceSelectionMessage> selections)
    {
        var sourceLimit = selections
            .Where(x => x.MaxChunks.HasValue && x.MaxChunks.Value > 0)
            .Sum(x => x.MaxChunks!.Value);

        var configuredLimit = Math.Max(1, configuration.MaximumChunksInFinalContext);
        var retrievalLimit = Math.Max(1, configuration.TopKRetrievalCount);
        var limit = sourceLimit > 0 ? sourceLimit : Math.Min(configuredLimit, retrievalLimit);

        return Math.Clamp(limit, 1, configuredLimit);
    }

    private static int ResolveBlobLimit(IReadOnlyList<TenantGenerationDataSourceSelectionMessage> selections)
        => ShouldIncludeBlobContent(selections) ? 3 : 0;

    private static string LimitContent(string value, int maxLength)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length <= maxLength)
            return normalized;

        return normalized[..Math.Max(1, maxLength)].TrimEnd() + "...";
    }

    private static IReadOnlyList<string> ExtractQueryTerms(string input)
        => (input ?? string.Empty)
            .ToLowerInvariant()
            .Split(
                [' ', '\r', '\n', '\t', '.', ',', ';', ':', '!', '?', '"', '\'', '(', ')', '[', ']', '{', '}', '/', '\\', '-', '_'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(term => term.Length >= 3)
            .Distinct(StringComparer.Ordinal)
            .Take(24)
            .ToList();

    private static string NormalizeLookup(string value)
        => string.Join(' ', (value ?? string.Empty)
            .Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToUpperInvariant();

    private static TenantKnowledgeConfigurationMessage MapConfiguration(TenantKnowledgeConfigurationDto configuration)
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
            new TenantKnowledgeModelConstraintsMessage(
                configuration.Models.EmbeddingProvider,
                configuration.Models.EmbeddingModel,
                configuration.Models.GenerationModel));

    private sealed record VectorChunkRetrievalResult(
        IReadOnlyList<RetrievedTenantGenerationSourceMessage> Sources,
        IReadOnlyList<TenantKnowledgeDocument> Documents);

    private sealed record ScoredTenantKnowledgeChunk(
        TenantKnowledgeDocument Document,
        TenantKnowledgeDocumentChunk Chunk,
        decimal? Score);
}

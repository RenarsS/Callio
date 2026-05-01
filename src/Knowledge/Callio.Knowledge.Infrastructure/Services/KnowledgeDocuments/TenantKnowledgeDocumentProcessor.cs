using Callio.Knowledge.Application.KnowledgeConfigurations;
using Callio.Knowledge.Domain;
using Callio.Provisioning.Infrastructure.Services;
using System.Text.Json;

namespace Callio.Knowledge.Infrastructure.Services.KnowledgeDocuments;

public class TenantKnowledgeDocumentProcessor(
    ITenantKnowledgeTextExtractor textExtractor,
    ITenantKnowledgeDocumentChunker chunker,
    ITenantEmbeddingGenerator embeddingGenerator,
    ITenantKnowledgeVectorStore vectorStore) : ITenantKnowledgeDocumentProcessor
{
    public async Task<TenantKnowledgeDocumentProcessingResult> ProcessAsync(
        TenantKnowledgeDocument document,
        TenantKnowledgeConfigurationDto configuration,
        TenantKnowledgeCategory? category,
        IReadOnlyList<TenantKnowledgeTag> tags,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        var extractedText = await textExtractor.ExtractTextAsync(
            document.OriginalFileName,
            content,
            cancellationToken);

        var chunks = chunker.Split(
            extractedText,
            new TenantKnowledgeChunkingOptions(configuration.ChunkSize, configuration.ChunkOverlap));

        if (chunks.Count == 0)
            throw new InvalidOperationException("No extractable text was found in the uploaded document.");

        var chunkContents = chunks.Select(x => x.Content).ToList();
        var embeddings = await embeddingGenerator.GenerateEmbeddingsAsync(
            chunkContents,
            configuration.Models.EmbeddingModel,
            cancellationToken);

        if (embeddings.Count != chunks.Count)
            throw new InvalidOperationException("The embedding generator returned an unexpected number of vectors.");

        var chunkEntities = chunks
            .Select((chunk, index) => new TenantKnowledgeDocumentChunk(
                chunk.ChunkIndex,
                BuildVectorRecordId(document.VectorStoreNamespace, document.DocumentKey, chunk.ChunkIndex),
                document.VectorStoreNamespace,
                configuration.Models.EmbeddingModel,
                embeddings[index].Length,
                chunk.Content,
                JsonSerializer.Serialize(embeddings[index]),
                DateTime.UtcNow))
            .ToList();

        var vectorRecords = BuildVectorRecords(document, category, tags, chunkEntities, embeddings);
        await vectorStore.IndexChunksAsync(document.VectorStoreNamespace, vectorRecords, cancellationToken);

        return new TenantKnowledgeDocumentProcessingResult(chunkEntities, vectorRecords);
    }

    private static string BuildVectorRecordId(string vectorNamespace, Guid documentKey, int chunkIndex)
        => $"{vectorNamespace}:{documentKey:N}:{chunkIndex:D4}";

    private static IReadOnlyList<TenantKnowledgeVectorRecord> BuildVectorRecords(
        TenantKnowledgeDocument document,
        TenantKnowledgeCategory? category,
        IReadOnlyList<TenantKnowledgeTag> tags,
        IReadOnlyList<TenantKnowledgeDocumentChunk> chunks,
        IReadOnlyList<float[]> embeddings)
    {
        var sectionKey = TenantVectorStoreCosmosContext.BuildSectionKey(category?.Id);
        var sectionName = TenantVectorStoreCosmosContext.BuildSectionName(category?.Name);
        var tagIds = tags.Select(x => x.Id).Distinct().ToList();
        var tagNames = tags.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        return chunks
            .Select((chunk, index) => new TenantKnowledgeVectorRecord(
                chunk.VectorRecordId,
                document.VectorStoreNamespace,
                document.DocumentKey.ToString("N"),
                chunk.ChunkIndex,
                sectionKey,
                sectionName,
                category?.Id,
                category?.Name,
                tagIds,
                tagNames,
                document.Title,
                document.BlobContainerName,
                document.BlobName,
                document.BlobUri,
                chunk.EmbeddingModel,
                chunk.Content,
                embeddings[index]))
            .ToList();
    }
}

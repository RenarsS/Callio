using Callio.Provisioning.Infrastructure.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json.Serialization;

namespace Callio.Knowledge.Infrastructure.Services.KnowledgeDocuments;

public class TenantKnowledgeVectorStore(
    TenantVectorStoreCosmosContext cosmosContext,
    ILogger<TenantKnowledgeVectorStore> logger) : ITenantKnowledgeVectorStore
{
    public bool UsesExternalVectorStore => cosmosContext.UsesAzureCosmos;

    public async Task IndexChunksAsync(
        string vectorStoreNamespace,
        IReadOnlyList<TenantKnowledgeVectorRecord> records,
        CancellationToken cancellationToken = default)
    {
        if (records.Count == 0)
            return;

        if (!UsesExternalVectorStore)
        {
            logger.LogWarning(
                "Skipping tenant knowledge vector indexing for namespace {VectorStoreNamespace} because Azure Cosmos vector storage is not enabled. Set TenantProvisioning:VectorStoreProvider to AzureCosmos.",
                vectorStoreNamespace);
            return;
        }

        var container = await cosmosContext.CreateVectorContainerIfNotExistsAsync(vectorStoreNamespace, cancellationToken);

        foreach (var sectionGroup in records.GroupBy(x => NormalizeRequired(x.SectionKey, nameof(TenantKnowledgeVectorRecord.SectionKey))))
        {
            var batch = container.CreateTransactionalBatch(new PartitionKey(sectionGroup.Key));
            foreach (var record in sectionGroup)
            {
                batch.UpsertItem(CosmosTenantKnowledgeVectorDocument.From(record));
            }

            var response = await batch.ExecuteAsync(cancellationToken);
            EnsureSuccessfulResponse(response, "upsert");
        }

        logger.LogInformation(
            "Indexed {VectorRecordCount} tenant knowledge vector records into Cosmos container {VectorStoreNamespace}.",
            records.Count,
            vectorStoreNamespace);
    }

    public async Task DeleteChunksAsync(
        string vectorStoreNamespace,
        string sectionKey,
        IReadOnlyList<string> vectorRecordIds,
        CancellationToken cancellationToken = default)
    {
        if (!UsesExternalVectorStore || vectorRecordIds.Count == 0)
            return;

        var container = cosmosContext.GetRequiredContainer(vectorStoreNamespace);
        var normalizedSectionKey = NormalizeRequired(sectionKey, nameof(sectionKey));
        var batch = container.CreateTransactionalBatch(new PartitionKey(normalizedSectionKey));

        foreach (var vectorRecordId in vectorRecordIds
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Select(x => x.Trim())
                     .Distinct(StringComparer.Ordinal))
        {
            batch.DeleteItem(vectorRecordId);
        }

        var response = await batch.ExecuteAsync(cancellationToken);
        EnsureSuccessfulResponse(response, "delete");
    }

    public async Task<IReadOnlyList<TenantKnowledgeVectorSearchResult>> SearchAsync(
        string vectorStoreNamespace,
        TenantKnowledgeVectorSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!UsesExternalVectorStore)
            return [];

        if (query.QueryVector is null || query.QueryVector.Length == 0)
            throw new ArgumentException("A query vector is required.", nameof(query));

        var container = cosmosContext.GetRequiredContainer(vectorStoreNamespace);
        var definition = BuildSearchQuery(query);
        var iterator = container.GetItemQueryIterator<CosmosTenantKnowledgeVectorSearchRow>(
            definition,
            requestOptions: new QueryRequestOptions
            {
                MaxItemCount = Math.Max(1, query.Top)
            });

        var results = new List<TenantKnowledgeVectorSearchResult>();
        while (iterator.HasMoreResults && results.Count < query.Top)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            foreach (var row in response)
            {
                if (!Guid.TryParseExact(row.DocumentKey, "N", out var documentKey))
                    continue;

                results.Add(new TenantKnowledgeVectorSearchResult(
                    row.Id,
                    documentKey,
                    row.ChunkIndex,
                    (decimal)Math.Round(row.Score, 6)));

                if (results.Count >= query.Top)
                    break;
            }
        }

        return results
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.DocumentKey)
            .ThenBy(x => x.ChunkIndex)
            .ToList();
    }

    private static QueryDefinition BuildSearchQuery(TenantKnowledgeVectorSearchQuery query)
    {
        var sql = new StringBuilder();
        sql.Append("SELECT TOP @top c.id, c.documentKey, c.chunkIndex, ");
        sql.Append("VectorDistance(c.contentVector, @embedding) AS score ");
        sql.Append("FROM c");

        var filters = new List<string>();
        var parameters = new List<KeyValuePair<string, object?>>
        {
            new("@top", Math.Max(1, query.Top)),
            new("@embedding", query.QueryVector)
        };

        AppendEqualityFilter(filters, parameters, "c.sectionKey", "@sectionKey", query.SectionKeys);
        AppendEqualityFilter(filters, parameters, "c.documentKey", "@documentKey", query.DocumentKeys);
        AppendEqualityFilter(filters, parameters, "c.categoryId", "@categoryId", query.CategoryIds);
        AppendArrayContainsFilter(filters, parameters, "c.tagIds", "@tagId", query.TagIds);

        if (filters.Count > 0)
        {
            sql.Append(" WHERE ");
            sql.Append(string.Join(" AND ", filters));
        }

        sql.Append(" ORDER BY VectorDistance(c.contentVector, @embedding)");

        var definition = new QueryDefinition(sql.ToString());
        foreach (var parameter in parameters)
        {
            definition.WithParameter(parameter.Key, parameter.Value);
        }

        return definition;
    }

    private static void AppendEqualityFilter<T>(
        ICollection<string> filters,
        ICollection<KeyValuePair<string, object?>> parameters,
        string fieldName,
        string parameterPrefix,
        IReadOnlyList<T> values)
    {
        var normalized = (values ?? [])
            .Where(x => x is not null)
            .Distinct()
            .ToList();

        if (normalized.Count == 0)
            return;

        var clauses = new List<string>(normalized.Count);
        for (var i = 0; i < normalized.Count; i++)
        {
            var parameterName = $"{parameterPrefix}{i}";
            clauses.Add($"{fieldName} = {parameterName}");
            parameters.Add(new KeyValuePair<string, object?>(parameterName, normalized[i]));
        }

        filters.Add("(" + string.Join(" OR ", clauses) + ")");
    }

    private static void AppendArrayContainsFilter(
        ICollection<string> filters,
        ICollection<KeyValuePair<string, object?>> parameters,
        string fieldName,
        string parameterPrefix,
        IReadOnlyList<int> values)
    {
        var normalized = (values ?? [])
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        if (normalized.Count == 0)
            return;

        var clauses = new List<string>(normalized.Count);
        for (var i = 0; i < normalized.Count; i++)
        {
            var parameterName = $"{parameterPrefix}{i}";
            clauses.Add($"ARRAY_CONTAINS({fieldName}, {parameterName})");
            parameters.Add(new KeyValuePair<string, object?>(parameterName, normalized[i]));
        }

        filters.Add("(" + string.Join(" OR ", clauses) + ")");
    }

    private static void EnsureSuccessfulResponse(TransactionalBatchResponse response, string operation)
    {
        if (response.IsSuccessStatusCode)
            return;

        throw new InvalidOperationException(
            $"Azure Cosmos vector {operation} failed with status code {(int)response.StatusCode} ({response.StatusCode}).");
    }

    private static string NormalizeRequired(string value, string fieldName)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
            throw new ArgumentException($"{fieldName} is required.", fieldName);

        return normalized;
    }

    private sealed class CosmosTenantKnowledgeVectorDocument
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("vectorStoreNamespace")]
        public string VectorStoreNamespace { get; init; } = string.Empty;

        [JsonPropertyName("documentKey")]
        public string DocumentKey { get; init; } = string.Empty;

        [JsonPropertyName("chunkIndex")]
        public int ChunkIndex { get; init; }

        [JsonPropertyName("sectionKey")]
        public string SectionKey { get; init; } = string.Empty;

        [JsonPropertyName("sectionName")]
        public string SectionName { get; init; } = string.Empty;

        [JsonPropertyName("categoryId")]
        public int? CategoryId { get; init; }

        [JsonPropertyName("categoryName")]
        public string? CategoryName { get; init; }

        [JsonPropertyName("tagIds")]
        public int[] TagIds { get; init; } = [];

        [JsonPropertyName("tagNames")]
        public string[] TagNames { get; init; } = [];

        [JsonPropertyName("documentTitle")]
        public string DocumentTitle { get; init; } = string.Empty;

        [JsonPropertyName("blobContainerName")]
        public string BlobContainerName { get; init; } = string.Empty;

        [JsonPropertyName("blobName")]
        public string BlobName { get; init; } = string.Empty;

        [JsonPropertyName("blobUri")]
        public string BlobUri { get; init; } = string.Empty;

        [JsonPropertyName("embeddingModel")]
        public string EmbeddingModel { get; init; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; init; } = string.Empty;

        [JsonPropertyName("contentVector")]
        public float[] ContentVector { get; init; } = [];

        public static CosmosTenantKnowledgeVectorDocument From(TenantKnowledgeVectorRecord record)
            => new()
            {
                Id = record.Id,
                VectorStoreNamespace = record.VectorStoreNamespace,
                DocumentKey = record.DocumentKey,
                ChunkIndex = record.ChunkIndex,
                SectionKey = record.SectionKey,
                SectionName = record.SectionName,
                CategoryId = record.CategoryId,
                CategoryName = record.CategoryName,
                TagIds = (record.TagIds ?? []).Where(x => x > 0).Distinct().ToArray(),
                TagNames = (record.TagNames ?? [])
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                DocumentTitle = record.DocumentTitle,
                BlobContainerName = record.BlobContainerName,
                BlobName = record.BlobName,
                BlobUri = record.BlobUri,
                EmbeddingModel = record.EmbeddingModel,
                Content = record.Content,
                ContentVector = record.ContentVector
            };
    }

    private sealed class CosmosTenantKnowledgeVectorSearchRow
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("documentKey")]
        public string DocumentKey { get; init; } = string.Empty;

        [JsonPropertyName("chunkIndex")]
        public int ChunkIndex { get; init; }

        [JsonPropertyName("score")]
        public double Score { get; init; }
    }
}

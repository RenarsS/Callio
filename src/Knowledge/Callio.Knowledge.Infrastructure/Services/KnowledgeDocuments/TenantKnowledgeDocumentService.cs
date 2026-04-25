using Callio.Admin.Infrastructure.Persistence;
using Callio.Knowledge.Application.KnowledgeConfigurations;
using Callio.Knowledge.Application.KnowledgeDocuments;
using Callio.Knowledge.Domain;
using Callio.Knowledge.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Callio.Knowledge.Infrastructure.Services.KnowledgeDocuments;

public class TenantKnowledgeDocumentService(
    ITenantKnowledgeDocumentRepository repository,
    ITenantKnowledgeConfigurationService knowledgeConfigurationService,
    ITenantKnowledgeBlobStorage blobStorage,
    ITenantKnowledgeTextExtractor textExtractor,
    ITenantEmbeddingGenerator embeddingGenerator,
    AdminDbContext adminDbContext,
    ILogger<TenantKnowledgeDocumentService> logger) : ITenantKnowledgeDocumentService
{
    public async Task<TenantKnowledgeCategoryDto> CreateCategoryAsync(
        CreateTenantKnowledgeCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.TenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(command.TenantId), "Tenant id must be greater than zero.");

        var existing = await repository.GetCategoryByNameAsync(command.TenantId, command.Name, cancellationToken);
        if (existing is not null)
            return existing.ToDto();

        var category = new TenantKnowledgeCategory(command.TenantId, command.Name, command.Description, DateTime.UtcNow);
        category = await repository.AddCategoryAsync(category, cancellationToken);

        return category.ToDto();
    }

    public async Task<IReadOnlyList<TenantKnowledgeCategoryDto>> GetCategoriesAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var categories = await repository.GetCategoriesAsync(tenantId, cancellationToken);
        return categories.Select(x => x.ToDto()).ToList();
    }

    public async Task<TenantKnowledgeTagDto> CreateTagAsync(
        CreateTenantKnowledgeTagCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.TenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(command.TenantId), "Tenant id must be greater than zero.");

        var existing = await repository.GetTagByNameAsync(command.TenantId, command.Name, cancellationToken);
        if (existing is not null)
            return existing.ToDto();

        var tag = new TenantKnowledgeTag(command.TenantId, command.Name, DateTime.UtcNow);
        tag = await repository.AddTagAsync(tag, cancellationToken);

        return tag.ToDto();
    }

    public async Task<IReadOnlyList<TenantKnowledgeTagDto>> GetTagsAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var tags = await repository.GetTagsAsync(tenantId, cancellationToken);
        return tags.Select(x => x.ToDto()).ToList();
    }

    public Task<TenantKnowledgeDocumentDto> UploadAsync(
        UploadTenantKnowledgeDocumentCommand command,
        CancellationToken cancellationToken = default)
        => UploadInternalAsync(command, cancellationToken);

    public async Task<TenantKnowledgeDocumentDto> UploadByTenantKeyAsync(
        Guid tenantKey,
        UploadTenantKnowledgeDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (tenantKey == Guid.Empty)
            throw new ArgumentException("Tenant key is required.", nameof(tenantKey));

        var tenantId = await adminDbContext.Tenants
            .AsNoTracking()
            .Where(x => x.TenantCode == tenantKey)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (tenantId <= 0)
            throw new InvalidOperationException("Tenant key is invalid.");

        return await UploadInternalAsync(command with { TenantId = tenantId }, cancellationToken);
    }

    public async Task<IReadOnlyList<TenantKnowledgeDocumentDto>> GetDocumentsAsync(
        int tenantId,
        GetTenantKnowledgeDocumentsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantId), "Tenant id must be greater than zero.");

        var status = TenantKnowledgeDocumentMappings.ParseStatus(query.Status);
        if (!string.IsNullOrWhiteSpace(query.Status) && status is null)
            throw new ArgumentException("Document status filter is invalid.", nameof(query.Status));

        var documents = await repository.GetDocumentsAsync(
            tenantId,
            query.CategoryId,
            query.TagId,
            status,
            cancellationToken);

        return documents.Select(x => x.ToDto()).ToList();
    }

    public async Task<TenantKnowledgeDocumentDto?> GetByIdAsync(int tenantId, int documentId, CancellationToken cancellationToken = default)
    {
        var document = await repository.GetByIdAsync(tenantId, documentId, cancellationToken);
        return document?.ToDto();
    }

    private async Task<TenantKnowledgeDocumentDto> UploadInternalAsync(
        UploadTenantKnowledgeDocumentCommand command,
        CancellationToken cancellationToken)
    {
        if (command.TenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(command.TenantId), "Tenant id must be greater than zero.");

        if (command.Content is null || command.Content.Length == 0)
            throw new ArgumentException("File content is required.", nameof(command.Content));

        var configuration = await GetOrCreateConfigurationAsync(command.TenantId, cancellationToken);
        var fileExtension = ValidateUploadAgainstConfiguration(command, configuration);
        var contentType = string.IsNullOrWhiteSpace(command.ContentType)
            ? "application/octet-stream"
            : command.ContentType.Trim();

        var category = await ResolveCategoryAsync(command, cancellationToken);
        var tags = await ResolveTagsAsync(command, cancellationToken);
        var vectorNamespace = await repository.ResolveVectorNamespaceAsync(command.TenantId, cancellationToken);
        var contentHash = ComputeSha256(command.Content);

        var blobMetadata = BuildBlobMetadata(command, category, tags);
        var blob = await blobStorage.UploadAsync(
            command.TenantId,
            command.FileName,
            contentType,
            command.Content,
            blobMetadata,
            cancellationToken);

        var document = new TenantKnowledgeDocument(
            command.TenantId,
            configuration.Id,
            category?.Id,
            ResolveTitle(command.Title, command.FileName),
            command.FileName,
            contentType,
            fileExtension,
            command.Content.LongLength,
            blob.ContainerName,
            blob.BlobName,
            blob.BlobUri,
            contentHash,
            vectorNamespace,
            command.SourceType,
            command.UploadedByUserId,
            command.UploadedByDisplayName,
            DateTime.UtcNow);

        document.AssignTags(tags.Select(x => x.Id), DateTime.UtcNow);

        var requiresApproval = configuration.ManualApprovalRequiredBeforeIndexing && !command.ApproveForIndexing;
        if (requiresApproval)
        {
            document.MarkAwaitingApproval(DateTime.UtcNow);
        }
        else
        {
            try
            {
                var extractedText = await textExtractor.ExtractTextAsync(command.FileName, command.Content, cancellationToken);
                var chunks = SplitIntoChunks(extractedText, configuration.ChunkSize, configuration.ChunkOverlap);

                if (chunks.Count == 0)
                    throw new InvalidOperationException("No extractable text was found in the uploaded document.");

                var embeddings = await embeddingGenerator.GenerateEmbeddingsAsync(
                    chunks,
                    configuration.Models.EmbeddingModel,
                    cancellationToken);

                if (embeddings.Count != chunks.Count)
                    throw new InvalidOperationException("The embedding generator returned an unexpected number of vectors.");

                var chunkEntities = chunks
                    .Select((chunk, index) => new TenantKnowledgeDocumentChunk(
                        index,
                        BuildVectorRecordId(vectorNamespace, document.DocumentKey, index),
                        vectorNamespace,
                        configuration.Models.EmbeddingModel,
                        embeddings[index].Length,
                        chunk,
                        JsonSerializer.Serialize(embeddings[index]),
                        DateTime.UtcNow))
                    .ToList();

                document.MarkReady(chunkEntities, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Knowledge document upload for tenant {TenantId} completed blob storage but failed during extraction or embedding.",
                    command.TenantId);

                document.MarkFailed(ex.GetBaseException().Message, DateTime.UtcNow);
            }
        }

        var saved = await repository.AddDocumentAsync(document, cancellationToken);
        return saved.ToDto();
    }

    private async Task<TenantKnowledgeConfigurationDto> GetOrCreateConfigurationAsync(int tenantId, CancellationToken cancellationToken)
    {
        var active = await knowledgeConfigurationService.GetActiveAsync(tenantId, cancellationToken);
        if (active is not null)
            return active;

        return await knowledgeConfigurationService.CreateDefaultAsync(
            new CreateDefaultTenantKnowledgeConfigurationCommand(tenantId),
            cancellationToken);
    }

    private static string ValidateUploadAgainstConfiguration(
        UploadTenantKnowledgeDocumentCommand command,
        TenantKnowledgeConfigurationDto configuration)
    {
        if (command.Content.LongLength > configuration.MaximumFileSizeBytes)
            throw new InvalidOperationException($"The file exceeds the tenant upload limit of {configuration.MaximumFileSizeBytes} bytes.");

        var extension = Path.GetExtension(command.FileName)?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension))
            throw new InvalidOperationException("The uploaded file must include an extension.");

        if (!configuration.AllowedFileTypes.Contains(extension, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Files with extension '{extension}' are not allowed for this tenant.");

        return extension;
    }

    private async Task<TenantKnowledgeCategory?> ResolveCategoryAsync(
        UploadTenantKnowledgeDocumentCommand command,
        CancellationToken cancellationToken)
    {
        if (command.CategoryId.HasValue)
        {
            var category = await repository.GetCategoryByIdAsync(command.TenantId, command.CategoryId.Value, cancellationToken);
            return category ?? throw new InvalidOperationException("The selected category was not found.");
        }

        if (string.IsNullOrWhiteSpace(command.CategoryName))
            return null;

        var existing = await repository.GetCategoryByNameAsync(command.TenantId, command.CategoryName, cancellationToken);
        if (existing is not null)
            return existing;

        return await repository.AddCategoryAsync(
            new TenantKnowledgeCategory(command.TenantId, command.CategoryName, null, DateTime.UtcNow),
            cancellationToken);
    }

    private async Task<IReadOnlyList<TenantKnowledgeTag>> ResolveTagsAsync(
        UploadTenantKnowledgeDocumentCommand command,
        CancellationToken cancellationToken)
    {
        var results = new List<TenantKnowledgeTag>();

        foreach (var tagId in (command.TagIds ?? []).Where(x => x > 0).Distinct())
        {
            var tag = await repository.GetTagByIdAsync(command.TenantId, tagId, cancellationToken)
                      ?? throw new InvalidOperationException($"The selected tag '{tagId}' was not found.");

            results.Add(tag);
        }

        foreach (var tagName in (command.TagNames ?? [])
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Select(x => x.Trim())
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var existing = await repository.GetTagByNameAsync(command.TenantId, tagName, cancellationToken);
            if (existing is not null)
            {
                results.Add(existing);
                continue;
            }

            results.Add(await repository.AddTagAsync(
                new TenantKnowledgeTag(command.TenantId, tagName, DateTime.UtcNow),
                cancellationToken));
        }

        return results
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .ToList();
    }

    private static IReadOnlyDictionary<string, string> BuildBlobMetadata(
        UploadTenantKnowledgeDocumentCommand command,
        TenantKnowledgeCategory? category,
        IReadOnlyList<TenantKnowledgeTag> tags)
        => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["tenantid"] = command.TenantId.ToString(),
            ["source"] = command.SourceType.ToString(),
            ["title"] = ResolveTitle(command.Title, command.FileName),
            ["category"] = category?.Name ?? string.Empty,
            ["tags"] = string.Join(',', tags.Select(x => x.Name))
        };

    private static string ResolveTitle(string? title, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(title))
            return title.Trim();

        var inferred = Path.GetFileNameWithoutExtension(fileName)?.Trim();
        if (!string.IsNullOrWhiteSpace(inferred))
            return inferred;

        return Path.GetFileName(fileName);
    }

    private static string ComputeSha256(byte[] content)
    {
        var hash = SHA256.HashData(content);
        var builder = new StringBuilder(hash.Length * 2);
        foreach (var value in hash)
        {
            builder.Append(value.ToString("x2"));
        }

        return builder.ToString();
    }

    private static string BuildVectorRecordId(string vectorNamespace, Guid documentKey, int chunkIndex)
        => $"{vectorNamespace}:{documentKey:N}:{chunkIndex:D4}";

    private static List<string> SplitIntoChunks(string text, int chunkSize, int chunkOverlap)
    {
        var normalized = (text ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Trim();

        if (normalized.Length == 0)
            return [];

        var chunks = new List<string>();
        var start = 0;

        while (start < normalized.Length)
        {
            var desiredEnd = Math.Min(normalized.Length, start + chunkSize);
            var end = desiredEnd;

            if (desiredEnd < normalized.Length)
            {
                var minBoundary = Math.Max(start + (chunkSize / 2), start + 1);
                for (var cursor = desiredEnd; cursor > minBoundary; cursor--)
                {
                    if (!char.IsWhiteSpace(normalized[cursor - 1]))
                        continue;

                    end = cursor;
                    break;
                }
            }

            if (end <= start)
                end = Math.Min(normalized.Length, start + chunkSize);

            var chunk = normalized[start..end].Trim();
            if (!string.IsNullOrWhiteSpace(chunk))
                chunks.Add(chunk);

            if (end >= normalized.Length)
                break;

            var nextStart = Math.Max(end - chunkOverlap, start + 1);
            while (nextStart < normalized.Length && char.IsWhiteSpace(normalized[nextStart]))
            {
                nextStart++;
            }

            start = nextStart;
        }

        return chunks;
    }
}

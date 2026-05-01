using Callio.Admin.Infrastructure.Persistence;
using Callio.Knowledge.Application.KnowledgeConfigurations;
using Callio.Knowledge.Application.KnowledgeDocuments;
using Callio.Knowledge.Domain;
using Callio.Knowledge.Domain.Enums;
using Callio.Provisioning.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Callio.Knowledge.Infrastructure.Services.KnowledgeDocuments;

public class TenantKnowledgeDocumentService(
    ITenantKnowledgeDocumentRepository repository,
    ITenantKnowledgeConfigurationService knowledgeConfigurationService,
    ITenantKnowledgeBlobStorage blobStorage,
    ITenantKnowledgeFileMetadataFactory metadataFactory,
    ITenantKnowledgeDocumentProcessor documentProcessor,
    ITenantKnowledgeVectorStore vectorStore,
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
        var category = await ResolveCategoryAsync(command, cancellationToken);
        var tags = await ResolveTagsAsync(command, cancellationToken);
        var vectorNamespace = await repository.ResolveVectorNamespaceAsync(command.TenantId, cancellationToken);
        var sectionKey = TenantVectorStoreCosmosContext.BuildSectionKey(category?.Id);
        var metadata = metadataFactory.Create(command, category, tags);

        var blob = await blobStorage.UploadAsync(
            command.TenantId,
            command.FileName,
            metadata.ContentType,
            command.Content,
            metadata.BlobMetadata,
            cancellationToken);

        var document = new TenantKnowledgeDocument(
            command.TenantId,
            configuration.Id,
            category?.Id,
            metadata.Title,
            command.FileName,
            metadata.ContentType,
            fileExtension,
            command.Content.LongLength,
            blob.ContainerName,
            blob.BlobName,
            blob.BlobUri,
            metadata.ContentHash,
            vectorNamespace,
            command.SourceType,
            command.UploadedByUserId,
            command.UploadedByDisplayName,
            DateTime.UtcNow);

        document.AssignTags(tags.Select(x => x.Id), DateTime.UtcNow);
        IReadOnlyList<TenantKnowledgeVectorRecord> vectorRecords = [];

        var requiresApproval = configuration.ManualApprovalRequiredBeforeIndexing && !command.ApproveForIndexing;
        if (requiresApproval)
        {
            document.MarkAwaitingApproval(DateTime.UtcNow);
            var awaitingApproval = await repository.AddDocumentAsync(document, cancellationToken);
            return awaitingApproval.ToDto();
        }

        var saved = await repository.AddDocumentAsync(document, cancellationToken);
        try
        {
            var processingResult = await documentProcessor.ProcessAsync(
                saved,
                configuration,
                category,
                tags,
                command.Content,
                cancellationToken);

            vectorRecords = processingResult.VectorRecords;
            saved.MarkReady(processingResult.Chunks, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Knowledge document upload for tenant {TenantId} completed blob storage but failed during extraction, chunking, embedding, or vector indexing.",
                command.TenantId);

            saved.MarkFailed(ex.GetBaseException().Message, DateTime.UtcNow);
        }

        try
        {
            var updated = await repository.UpdateDocumentAsync(saved, cancellationToken);
            return updated.ToDto();
        }
        catch
        {
            if (saved.ProcessingStatus == KnowledgeDocumentProcessingStatus.Ready && vectorRecords.Count > 0)
            {
                try
                {
                    await vectorStore.DeleteChunksAsync(
                        vectorNamespace,
                        sectionKey,
                        vectorRecords.Select(x => x.Id).ToList(),
                        cancellationToken);
                }
                catch (Exception cleanupException)
                {
                    logger.LogWarning(
                        cleanupException,
                        "Knowledge document persistence failed after vector indexing for tenant {TenantId}. Vector cleanup for document {DocumentKey} may be incomplete.",
                        command.TenantId,
                        saved.DocumentKey);
                }
            }

            throw;
        }
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

}

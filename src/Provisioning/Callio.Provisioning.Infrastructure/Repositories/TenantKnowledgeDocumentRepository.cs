using Callio.Provisioning.Application.KnowledgeDocuments;
using Callio.Provisioning.Domain;
using Callio.Provisioning.Domain.Enums;
using Callio.Provisioning.Infrastructure.Persistence;
using Callio.Provisioning.Infrastructure.Provisioners;
using Callio.Provisioning.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Callio.Provisioning.Infrastructure.Repositories;

public class TenantKnowledgeDocumentRepository(
    ProvisioningDbContext provisioningDbContext,
    ITenantResourceNamingStrategy tenantResourceNamingStrategy,
    ITenantKnowledgeDocumentDbContextFactory dbContextFactory,
    ITenantKnowledgeDocumentStoreProvisioner storeProvisioner) : ITenantKnowledgeDocumentRepository
{
    public async Task<TenantKnowledgeCategory?> GetCategoryByIdAsync(int tenantId, int categoryId, CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(tenantId, cancellationToken);
        return await context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == categoryId, cancellationToken);
    }

    public async Task<TenantKnowledgeCategory?> GetCategoryByNameAsync(int tenantId, string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeLookup(name);

        await using var context = await CreateContextAsync(tenantId, cancellationToken);
        return await context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.NormalizedName == normalizedName, cancellationToken);
    }

    public async Task<IReadOnlyList<TenantKnowledgeCategory>> GetCategoriesAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(tenantId, cancellationToken);
        return await context.Categories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantKnowledgeCategory> AddCategoryAsync(TenantKnowledgeCategory category, CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(category.TenantId, cancellationToken);
        context.Categories.Add(category);
        await context.SaveChangesAsync(cancellationToken);
        return category;
    }

    public async Task<TenantKnowledgeTag?> GetTagByIdAsync(int tenantId, int tagId, CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(tenantId, cancellationToken);
        return await context.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tagId, cancellationToken);
    }

    public async Task<TenantKnowledgeTag?> GetTagByNameAsync(int tenantId, string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeLookup(name);

        await using var context = await CreateContextAsync(tenantId, cancellationToken);
        return await context.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.NormalizedName == normalizedName, cancellationToken);
    }

    public async Task<IReadOnlyList<TenantKnowledgeTag>> GetTagsAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(tenantId, cancellationToken);
        return await context.Tags
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantKnowledgeTag> AddTagAsync(TenantKnowledgeTag tag, CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(tag.TenantId, cancellationToken);
        context.Tags.Add(tag);
        await context.SaveChangesAsync(cancellationToken);
        return tag;
    }

    public async Task<TenantKnowledgeDocument?> GetByIdAsync(int tenantId, int documentId, CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(tenantId, cancellationToken);
        return await context.Documents
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.DocumentTags)
                .ThenInclude(x => x.Tag)
            .Include(x => x.Chunks)
            .FirstOrDefaultAsync(x => x.Id == documentId, cancellationToken);
    }

    public async Task<IReadOnlyList<TenantKnowledgeDocument>> GetDocumentsAsync(
        int tenantId,
        int? categoryId,
        int? tagId,
        KnowledgeDocumentProcessingStatus? status,
        CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(tenantId, cancellationToken);

        var query = context.Documents
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.DocumentTags)
                .ThenInclude(x => x.Tag)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(x => x.CategoryId == categoryId.Value);

        if (tagId.HasValue)
            query = query.Where(x => x.DocumentTags.Any(tag => tag.TenantKnowledgeTagId == tagId.Value));

        if (status.HasValue)
            query = query.Where(x => x.ProcessingStatus == status.Value);

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantKnowledgeDocument> AddDocumentAsync(TenantKnowledgeDocument document, CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(document.TenantId, cancellationToken);
        context.Documents.Add(document);
        await context.SaveChangesAsync(cancellationToken);

        return await context.Documents
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.DocumentTags)
                .ThenInclude(x => x.Tag)
            .Include(x => x.Chunks)
            .FirstAsync(x => x.Id == document.Id, cancellationToken);
    }

    public async Task<string> ResolveVectorNamespaceAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var provisioning = await provisioningDbContext.TenantInfrastructureProvisionings
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.VectorStoreNamespace)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(provisioning))
            return provisioning;

        return tenantResourceNamingStrategy.Create(tenantId).VectorStoreNamespace;
    }

    private async Task<TenantKnowledgeDocumentDbContext> CreateContextAsync(int tenantId, CancellationToken cancellationToken)
    {
        var schemaName = await ResolveSchemaNameAsync(tenantId, cancellationToken);
        await storeProvisioner.EnsureCreatedAsync(schemaName, cancellationToken);
        return dbContextFactory.Create(schemaName);
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

    private static string NormalizeLookup(string value)
        => string.Join(' ', (value ?? string.Empty)
            .Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToUpperInvariant();
}

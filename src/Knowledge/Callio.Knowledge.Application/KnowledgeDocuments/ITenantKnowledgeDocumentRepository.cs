using Callio.Knowledge.Domain;
using Callio.Knowledge.Domain.Enums;

namespace Callio.Knowledge.Application.KnowledgeDocuments;

public interface ITenantKnowledgeDocumentRepository
{
    Task<TenantKnowledgeCategory?> GetCategoryByIdAsync(int tenantId, int categoryId, CancellationToken cancellationToken = default);

    Task<TenantKnowledgeCategory?> GetCategoryByNameAsync(int tenantId, string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantKnowledgeCategory>> GetCategoriesAsync(int tenantId, CancellationToken cancellationToken = default);

    Task<TenantKnowledgeCategory> AddCategoryAsync(TenantKnowledgeCategory category, CancellationToken cancellationToken = default);

    Task<TenantKnowledgeTag?> GetTagByIdAsync(int tenantId, int tagId, CancellationToken cancellationToken = default);

    Task<TenantKnowledgeTag?> GetTagByNameAsync(int tenantId, string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantKnowledgeTag>> GetTagsAsync(int tenantId, CancellationToken cancellationToken = default);

    Task<TenantKnowledgeTag> AddTagAsync(TenantKnowledgeTag tag, CancellationToken cancellationToken = default);

    Task<TenantKnowledgeDocument?> GetByIdAsync(int tenantId, int documentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantKnowledgeDocument>> GetDocumentsAsync(
        int tenantId,
        int? categoryId,
        int? tagId,
        KnowledgeDocumentProcessingStatus? status,
        CancellationToken cancellationToken = default);

    Task<TenantKnowledgeDocument> AddDocumentAsync(TenantKnowledgeDocument document, CancellationToken cancellationToken = default);

    Task<TenantKnowledgeDocument> UpdateDocumentAsync(TenantKnowledgeDocument document, CancellationToken cancellationToken = default);

    Task<string> ResolveVectorNamespaceAsync(int tenantId, CancellationToken cancellationToken = default);
}

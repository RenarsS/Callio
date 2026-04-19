namespace Callio.Provisioning.Application.KnowledgeDocuments;

public interface ITenantKnowledgeDocumentService
{
    Task<TenantKnowledgeCategoryDto> CreateCategoryAsync(
        CreateTenantKnowledgeCategoryCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantKnowledgeCategoryDto>> GetCategoriesAsync(int tenantId, CancellationToken cancellationToken = default);

    Task<TenantKnowledgeTagDto> CreateTagAsync(
        CreateTenantKnowledgeTagCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantKnowledgeTagDto>> GetTagsAsync(int tenantId, CancellationToken cancellationToken = default);

    Task<TenantKnowledgeDocumentDto> UploadAsync(
        UploadTenantKnowledgeDocumentCommand command,
        CancellationToken cancellationToken = default);

    Task<TenantKnowledgeDocumentDto> UploadByTenantKeyAsync(
        Guid tenantKey,
        UploadTenantKnowledgeDocumentCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantKnowledgeDocumentDto>> GetDocumentsAsync(
        int tenantId,
        GetTenantKnowledgeDocumentsQuery query,
        CancellationToken cancellationToken = default);

    Task<TenantKnowledgeDocumentDto?> GetByIdAsync(int tenantId, int documentId, CancellationToken cancellationToken = default);
}

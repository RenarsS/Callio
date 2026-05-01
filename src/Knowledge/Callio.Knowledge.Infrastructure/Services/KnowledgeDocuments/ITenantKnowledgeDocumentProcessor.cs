using Callio.Knowledge.Application.KnowledgeConfigurations;
using Callio.Knowledge.Domain;

namespace Callio.Knowledge.Infrastructure.Services.KnowledgeDocuments;

public interface ITenantKnowledgeDocumentProcessor
{
    Task<TenantKnowledgeDocumentProcessingResult> ProcessAsync(
        TenantKnowledgeDocument document,
        TenantKnowledgeConfigurationDto configuration,
        TenantKnowledgeCategory? category,
        IReadOnlyList<TenantKnowledgeTag> tags,
        byte[] content,
        CancellationToken cancellationToken = default);
}

public sealed record TenantKnowledgeDocumentProcessingResult(
    IReadOnlyList<TenantKnowledgeDocumentChunk> Chunks,
    IReadOnlyList<TenantKnowledgeVectorRecord> VectorRecords);

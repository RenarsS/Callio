using Callio.Knowledge.Application.KnowledgeDocuments;
using Callio.Knowledge.Domain;

namespace Callio.Knowledge.Infrastructure.Services.KnowledgeDocuments;

public interface ITenantKnowledgeFileMetadataFactory
{
    TenantKnowledgeFileMetadata Create(
        UploadTenantKnowledgeDocumentCommand command,
        TenantKnowledgeCategory? category,
        IReadOnlyList<TenantKnowledgeTag> tags);
}

public sealed record TenantKnowledgeFileMetadata(
    string Title,
    string ContentType,
    string FileExtension,
    string ContentHash,
    IReadOnlyDictionary<string, string> BlobMetadata);

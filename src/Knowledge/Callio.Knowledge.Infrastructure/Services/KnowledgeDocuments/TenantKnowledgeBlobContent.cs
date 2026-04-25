namespace Callio.Knowledge.Infrastructure.Services.KnowledgeDocuments;

public record TenantKnowledgeBlobContent(
    string ContainerName,
    string BlobName,
    string ContentType,
    byte[] Content);

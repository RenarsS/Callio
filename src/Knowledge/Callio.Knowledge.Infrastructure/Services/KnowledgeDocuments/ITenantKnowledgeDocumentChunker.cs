namespace Callio.Knowledge.Infrastructure.Services.KnowledgeDocuments;

public interface ITenantKnowledgeDocumentChunker
{
    IReadOnlyList<TenantKnowledgeDocumentChunkText> Split(
        string text,
        TenantKnowledgeChunkingOptions options);
}

public sealed record TenantKnowledgeChunkingOptions(
    int ChunkSize,
    int ChunkOverlap);

public sealed record TenantKnowledgeDocumentChunkText(
    int ChunkIndex,
    string Content);

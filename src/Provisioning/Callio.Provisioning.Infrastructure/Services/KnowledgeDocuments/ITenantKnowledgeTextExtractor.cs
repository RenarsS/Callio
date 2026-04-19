namespace Callio.Provisioning.Infrastructure.Services.KnowledgeDocuments;

public interface ITenantKnowledgeTextExtractor
{
    Task<string> ExtractTextAsync(string fileName, byte[] content, CancellationToken cancellationToken = default);
}

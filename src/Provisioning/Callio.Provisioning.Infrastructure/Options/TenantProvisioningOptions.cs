namespace Callio.Provisioning.Infrastructure.Options;

public sealed class TenantProvisioningOptions
{
    public const string SectionName = "TenantProvisioning";

    public string SchemaPrefix { get; set; } = "tenant_";

    public string VectorNamespacePrefix { get; set; } = "tenant-";

    public string EmbeddingProvider { get; set; } = "openai";

    public string EmbeddingModel { get; set; } = "text-embedding-3-small";

    public int ChunkSize { get; set; } = 800;

    public int ChunkOverlap { get; set; } = 120;

    public int RetrievalTopK { get; set; } = 8;

    public bool EnableKnowledgeIngestion { get; set; } = true;

    public bool EnableKnowledgeRetrieval { get; set; } = true;
}

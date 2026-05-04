namespace Callio.Knowledge.Infrastructure.Options;

public sealed class TenantKnowledgeIngestionOptions
{
    public const string SectionName = "TenantKnowledgeIngestion";

    public string BlobProvider { get; set; } = "Local";

    public string LocalStorageRootPath { get; set; } = "App_Data\\tenant-knowledge";

    public string AzureBlobConnectionString { get; set; } = string.Empty;

    public string AzureBlobContainerName { get; set; } = "tenant-knowledge";

    public string EmbeddingProvider { get; set; } = "Deterministic";

    public int DeterministicEmbeddingDimensions { get; set; } = 128;

    public string OpenAIApiKey { get; set; } = string.Empty;

    public string OpenAIBaseUrl { get; set; } = "https://api.openai.com/v1";
}

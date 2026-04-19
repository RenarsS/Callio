namespace Callio.Provisioning.Infrastructure.Options;

public sealed class TenantKnowledgeIngestionOptions
{
    public const string SectionName = "TenantKnowledgeIngestion";

    public string BlobProvider { get; set; } = "Local";

    public string LocalStorageRootPath { get; set; } = "App_Data\\tenant-knowledge";

    public string AzureBlobConnectionString { get; set; } = string.Empty;

    public string AzureBlobContainerName { get; set; } = "tenant-knowledge";

    public string EmbeddingProvider { get; set; } = "Deterministic";

    public int DeterministicEmbeddingDimensions { get; set; } = 128;

    public string AzureOpenAIEndpoint { get; set; } = string.Empty;

    public string AzureOpenAIKey { get; set; } = string.Empty;

    public string AzureOpenAIEmbeddingDeployment { get; set; } = string.Empty;

    public string AzureOpenAIApiVersion { get; set; } = "2024-06-01";
}

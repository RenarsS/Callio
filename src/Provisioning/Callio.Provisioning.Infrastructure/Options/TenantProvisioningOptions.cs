namespace Callio.Provisioning.Infrastructure.Options;

public sealed class TenantProvisioningOptions
{
    public const string SectionName = "TenantProvisioning";

    public string SchemaPrefix { get; set; } = "tenant_";

    public string VectorNamespacePrefix { get; set; } = "tenant-";

    public string EmbeddingProvider { get; set; } = "openai";

    public string EmbeddingModel { get; set; } = "text-embedding-3-small";

    public string GenerationModel { get; set; } = "gpt-4.1-mini";

    public string DefaultSystemPrompt { get; set; } =
        "You are Callio's knowledge assistant. Answer using tenant-approved knowledge only and clearly state when the knowledge base does not contain the answer.";

    public string DefaultAssistantInstructionPrompt { get; set; } =
        "Retrieve the most relevant approved chunks, prioritize precision over coverage, and avoid unsupported claims.";

    public int ChunkSize { get; set; } = 800;

    public int ChunkOverlap { get; set; } = 120;

    public int RetrievalTopK { get; set; } = 8;

    public int MaximumChunksInFinalContext { get; set; } = 6;

    public decimal MinimumSimilarityThreshold { get; set; } = 0.7m;

    public string[] AllowedFileTypes { get; set; } = [".pdf", ".docx", ".txt", ".md"];

    public long MaximumFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    public bool AutoProcessOnUpload { get; set; } = true;

    public bool ManualApprovalRequiredBeforeIndexing { get; set; }

    public bool VersioningEnabled { get; set; } = true;
}

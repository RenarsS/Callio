namespace Callio.Knowledge.Infrastructure.Options;

public sealed class TenantGenerationSourceRetrievalOptions
{
    public const string SectionName = "TenantGeneration";

    public int SourceExcerptMaxCharacters { get; set; } = 1800;

    public int BlobContentMaxCharacters { get; set; } = 3000;
}

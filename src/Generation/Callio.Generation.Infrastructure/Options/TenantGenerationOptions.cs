namespace Callio.Generation.Infrastructure.Options;

public sealed class TenantGenerationOptions
{
    public const string SectionName = "TenantGeneration";

    public string CompletionProvider { get; set; } = "Deterministic";

    public string DefaultPromptKey { get; set; } = "knowledge-answer";

    public string GenerationModel { get; set; } = "gpt-4.1-mini";

    public int MaxOutputTokens { get; set; } = 800;

    public decimal Temperature { get; set; } = 0.2m;

    public int SourceExcerptMaxCharacters { get; set; } = 1800;

    public int BlobContentMaxCharacters { get; set; } = 3000;

    public string AzureOpenAIEndpoint { get; set; } = string.Empty;

    public string AzureOpenAIKey { get; set; } = string.Empty;

    public string AzureOpenAIChatDeployment { get; set; } = string.Empty;

    public string AzureOpenAIApiVersion { get; set; } = "2024-06-01";

    public TenantGenerationPromptTemplateOptions[] PromptTemplates { get; set; } =
    [
        new()
        {
            Key = "knowledge-answer",
            Name = "Knowledge answer",
            Description = "Answer from tenant knowledge context.",
            SystemPrompt = "You are Callio's tenant knowledge generation assistant. Use only the supplied context. Say when the retrieved tenant knowledge does not contain enough information.",
            UserPromptTemplate = """
Question:
{{input}}

Tenant assistant instructions:
{{assistantInstructionPrompt}}

Retrieved context:
{{context}}

Write a direct answer and cite the source numbers that support it.
""",
            DataSources =
            [
                new()
                {
                    SourceKind = "KnowledgeChunk"
                }
            ]
        }
    ];
}

public sealed class TenantGenerationPromptTemplateOptions
{
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string SystemPrompt { get; set; } = string.Empty;

    public string UserPromptTemplate { get; set; } = string.Empty;

    public TenantGenerationDataSourceOptions[] DataSources { get; set; } = [];
}

public sealed class TenantGenerationDataSourceOptions
{
    public string SourceKind { get; set; } = "KnowledgeChunk";

    public int? CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public int? TagId { get; set; }

    public string? TagName { get; set; }

    public int? DocumentId { get; set; }

    public int? MaxChunks { get; set; }

    public bool IncludeBlobContent { get; set; }
}

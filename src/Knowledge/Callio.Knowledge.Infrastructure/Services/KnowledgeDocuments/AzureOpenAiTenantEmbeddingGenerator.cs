using Callio.Knowledge.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Callio.Knowledge.Infrastructure.Services.KnowledgeDocuments;

public class AzureOpenAiTenantEmbeddingGenerator(
    IOptions<TenantKnowledgeIngestionOptions> options) : ITenantEmbeddingGenerator
{
    private static readonly HttpClient HttpClient = new();
    private readonly TenantKnowledgeIngestionOptions _options = options.Value;

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> chunks,
        string embeddingModel,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AzureOpenAIEndpoint))
            throw new InvalidOperationException("Azure OpenAI endpoint is required.");

        if (string.IsNullOrWhiteSpace(_options.AzureOpenAIKey))
            throw new InvalidOperationException("Azure OpenAI key is required.");

        var deployment = string.IsNullOrWhiteSpace(_options.AzureOpenAIEmbeddingDeployment)
            ? embeddingModel
            : _options.AzureOpenAIEmbeddingDeployment.Trim();

        var endpoint = _options.AzureOpenAIEndpoint.TrimEnd('/');
        var apiVersion = string.IsNullOrWhiteSpace(_options.AzureOpenAIApiVersion)
            ? "2024-06-01"
            : _options.AzureOpenAIApiVersion.Trim();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{endpoint}/openai/deployments/{deployment}/embeddings?api-version={apiVersion}");

        request.Headers.Add("api-key", _options.AzureOpenAIKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { input = chunks }),
            Encoding.UTF8,
            "application/json");

        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Azure OpenAI embeddings response did not include an embeddings array.");

        var embeddings = dataElement
            .EnumerateArray()
            .OrderBy(x => x.TryGetProperty("index", out var indexElement) ? indexElement.GetInt32() : int.MaxValue)
            .Select(ParseEmbedding)
            .ToList();

        if (embeddings.Count != chunks.Count)
            throw new InvalidOperationException("Azure OpenAI returned a different number of embeddings than requested.");

        return embeddings;
    }

    private static float[] ParseEmbedding(JsonElement element)
    {
        if (!element.TryGetProperty("embedding", out var embeddingElement) || embeddingElement.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Azure OpenAI embedding item did not include an embedding vector.");

        return embeddingElement
            .EnumerateArray()
            .Select(x => x.GetSingle())
            .ToArray();
    }
}

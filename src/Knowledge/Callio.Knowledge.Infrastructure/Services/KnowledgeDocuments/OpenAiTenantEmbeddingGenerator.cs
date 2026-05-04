using Callio.Knowledge.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Callio.Knowledge.Infrastructure.Services.KnowledgeDocuments;

public class OpenAiTenantEmbeddingGenerator(
    IOptions<TenantKnowledgeIngestionOptions> options) : ITenantEmbeddingGenerator
{
    private static readonly HttpClient HttpClient = new();
    private readonly TenantKnowledgeIngestionOptions _options = options.Value;

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> chunks,
        string embeddingModel,
        CancellationToken cancellationToken = default)
    {
        var apiKey = ResolveApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key is required.");

        var model = string.IsNullOrWhiteSpace(embeddingModel)
            ? "text-embedding-3-small"
            : embeddingModel.Trim();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{ResolveBaseUrl()}/embeddings");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                model,
                input = chunks
            }),
            Encoding.UTF8,
            "application/json");

        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("OpenAI embeddings response did not include an embeddings array.");

        var embeddings = dataElement
            .EnumerateArray()
            .OrderBy(x => x.TryGetProperty("index", out var indexElement) ? indexElement.GetInt32() : int.MaxValue)
            .Select(ParseEmbedding)
            .ToList();

        if (embeddings.Count != chunks.Count)
            throw new InvalidOperationException("OpenAI returned a different number of embeddings than requested.");

        return embeddings;
    }

    private string ResolveBaseUrl()
        => string.IsNullOrWhiteSpace(_options.OpenAIBaseUrl)
            ? "https://api.openai.com/v1"
            : _options.OpenAIBaseUrl.TrimEnd('/');

    private string? ResolveApiKey()
        => string.IsNullOrWhiteSpace(_options.OpenAIApiKey)
            ? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            : _options.OpenAIApiKey;

    private static float[] ParseEmbedding(JsonElement element)
    {
        if (!element.TryGetProperty("embedding", out var embeddingElement) || embeddingElement.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("OpenAI embedding item did not include an embedding vector.");

        return embeddingElement
            .EnumerateArray()
            .Select(x => x.GetSingle())
            .ToArray();
    }
}

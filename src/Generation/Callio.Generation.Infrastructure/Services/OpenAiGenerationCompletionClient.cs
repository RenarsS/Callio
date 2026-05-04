using Callio.Generation.Application.Generation;
using Callio.Generation.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Callio.Generation.Infrastructure.Services;

public class OpenAiGenerationCompletionClient(IOptions<TenantGenerationOptions> options) : IGenerationCompletionClient
{
    private static readonly HttpClient HttpClient = new();
    private readonly TenantGenerationOptions _options = options.Value;

    public async Task<GenerationCompletionResultDto> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        string model,
        CancellationToken cancellationToken = default)
    {
        var apiKey = ResolveApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key is required for generation.");

        var resolvedModel = string.IsNullOrWhiteSpace(model)
            ? _options.GenerationModel
            : model.Trim();

        if (string.IsNullOrWhiteSpace(resolvedModel))
            throw new InvalidOperationException("OpenAI model is required for generation.");

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{ResolveBaseUrl()}/chat/completions");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                model = resolvedModel,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = _options.Temperature,
                max_tokens = _options.MaxOutputTokens
            }),
            Encoding.UTF8,
            "application/json");

        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

        var content = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("OpenAI generation response did not include content.");

        return new GenerationCompletionResultDto(
            content,
            resolvedModel,
            EstimateTokens(content));
    }

    private string ResolveBaseUrl()
        => string.IsNullOrWhiteSpace(_options.OpenAIBaseUrl)
            ? "https://api.openai.com/v1"
            : _options.OpenAIBaseUrl.TrimEnd('/');

    private string? ResolveApiKey()
        => string.IsNullOrWhiteSpace(_options.OpenAIApiKey)
            ? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            : _options.OpenAIApiKey;

    private static int EstimateTokens(string value)
        => Math.Max(1, (int)Math.Ceiling((value?.Length ?? 0) / 4d));
}

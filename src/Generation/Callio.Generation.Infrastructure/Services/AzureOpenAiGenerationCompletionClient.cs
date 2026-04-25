using Callio.Generation.Application.Generation;
using Callio.Generation.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Callio.Generation.Infrastructure.Services;

public class AzureOpenAiGenerationCompletionClient(IOptions<TenantGenerationOptions> options) : IGenerationCompletionClient
{
    private static readonly HttpClient HttpClient = new();
    private readonly TenantGenerationOptions _options = options.Value;

    public async Task<GenerationCompletionResultDto> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        string model,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AzureOpenAIEndpoint))
            throw new InvalidOperationException("Azure OpenAI endpoint is required for generation.");

        if (string.IsNullOrWhiteSpace(_options.AzureOpenAIKey))
            throw new InvalidOperationException("Azure OpenAI key is required for generation.");

        var deployment = string.IsNullOrWhiteSpace(_options.AzureOpenAIChatDeployment)
            ? model
            : _options.AzureOpenAIChatDeployment.Trim();

        if (string.IsNullOrWhiteSpace(deployment))
            throw new InvalidOperationException("Azure OpenAI chat deployment is required for generation.");

        var endpoint = _options.AzureOpenAIEndpoint.TrimEnd('/');
        var apiVersion = string.IsNullOrWhiteSpace(_options.AzureOpenAIApiVersion)
            ? "2024-06-01"
            : _options.AzureOpenAIApiVersion.Trim();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{endpoint}/openai/deployments/{deployment}/chat/completions?api-version={apiVersion}");

        request.Headers.Add("api-key", _options.AzureOpenAIKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
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
            throw new InvalidOperationException("Azure OpenAI generation response did not include content.");

        return new GenerationCompletionResultDto(
            content,
            deployment,
            EstimateTokens(content));
    }

    private static int EstimateTokens(string value)
        => Math.Max(1, (int)Math.Ceiling((value?.Length ?? 0) / 4d));
}

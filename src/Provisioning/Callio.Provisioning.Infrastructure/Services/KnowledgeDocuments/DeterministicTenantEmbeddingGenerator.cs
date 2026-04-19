using Callio.Provisioning.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Callio.Provisioning.Infrastructure.Services.KnowledgeDocuments;

public class DeterministicTenantEmbeddingGenerator(IOptions<TenantKnowledgeIngestionOptions> options) : ITenantEmbeddingGenerator
{
    private readonly TenantKnowledgeIngestionOptions _options = options.Value;

    public Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> chunks,
        string embeddingModel,
        CancellationToken cancellationToken = default)
    {
        var dimensions = Math.Max(16, _options.DeterministicEmbeddingDimensions);
        var embeddings = chunks
            .Select(chunk => CreateEmbedding(chunk, embeddingModel, dimensions))
            .ToList();

        return Task.FromResult<IReadOnlyList<float[]>>(embeddings);
    }

    private static float[] CreateEmbedding(string chunk, string embeddingModel, int dimensions)
    {
        var vector = new float[dimensions];
        var seed = Encoding.UTF8.GetBytes($"{embeddingModel}::{chunk}");
        using var sha = SHA256.Create();

        var offset = 0;
        var current = seed;
        while (offset < dimensions)
        {
            current = sha.ComputeHash(current);
            for (var i = 0; i < current.Length && offset < dimensions; i += 4)
            {
                var bytes = current.Skip(i).Take(4).ToArray();
                if (bytes.Length < 4)
                    break;

                var value = BitConverter.ToUInt32(bytes, 0) / (float)uint.MaxValue;
                vector[offset++] = (value * 2f) - 1f;
            }
        }

        var magnitude = MathF.Sqrt(vector.Sum(x => x * x));
        if (magnitude > 0f)
        {
            for (var i = 0; i < vector.Length; i++)
            {
                vector[i] /= magnitude;
            }
        }

        return vector;
    }
}

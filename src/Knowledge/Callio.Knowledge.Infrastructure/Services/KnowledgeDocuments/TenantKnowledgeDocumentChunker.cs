namespace Callio.Knowledge.Infrastructure.Services.KnowledgeDocuments;

public class TenantKnowledgeDocumentChunker : ITenantKnowledgeDocumentChunker
{
    public IReadOnlyList<TenantKnowledgeDocumentChunkText> Split(
        string text,
        TenantKnowledgeChunkingOptions options)
    {
        if (options.ChunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "Chunk size must be greater than zero.");

        if (options.ChunkOverlap < 0)
            throw new ArgumentOutOfRangeException(nameof(options), "Chunk overlap cannot be negative.");

        if (options.ChunkOverlap >= options.ChunkSize)
            throw new ArgumentOutOfRangeException(nameof(options), "Chunk overlap must be smaller than chunk size.");

        var normalized = (text ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Trim();

        if (normalized.Length == 0)
            return [];

        var chunks = new List<TenantKnowledgeDocumentChunkText>();
        var start = 0;

        while (start < normalized.Length)
        {
            var desiredEnd = Math.Min(normalized.Length, start + options.ChunkSize);
            var end = desiredEnd;

            if (desiredEnd < normalized.Length)
            {
                var minBoundary = Math.Max(start + (options.ChunkSize / 2), start + 1);
                for (var cursor = desiredEnd; cursor > minBoundary; cursor--)
                {
                    if (!char.IsWhiteSpace(normalized[cursor - 1]))
                        continue;

                    end = cursor;
                    break;
                }
            }

            if (end <= start)
                end = Math.Min(normalized.Length, start + options.ChunkSize);

            var chunk = normalized[start..end].Trim();
            if (!string.IsNullOrWhiteSpace(chunk))
                chunks.Add(new TenantKnowledgeDocumentChunkText(chunks.Count, chunk));

            if (end >= normalized.Length)
                break;

            var nextStart = Math.Max(end - options.ChunkOverlap, start + 1);
            while (nextStart < normalized.Length && char.IsWhiteSpace(normalized[nextStart]))
            {
                nextStart++;
            }

            start = nextStart;
        }

        return chunks;
    }
}

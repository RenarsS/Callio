using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Callio.Knowledge.Infrastructure.Services.KnowledgeDocuments;

public class TenantKnowledgeTextExtractor : ITenantKnowledgeTextExtractor
{
    private static readonly string[] PlainTextExtensions = [".txt", ".md", ".csv", ".json", ".jsonl", ".xml", ".html", ".htm"];

    public Task<string> ExtractTextAsync(string fileName, byte[] content, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName)?.Trim().ToLowerInvariant() ?? string.Empty;
        if (PlainTextExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return Task.FromResult(DecodeText(content));

        if (string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(ExtractDocxText(content));

        if (string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(ExtractPdfText(content));

        throw new NotSupportedException($"Files with extension '{extension}' are not currently supported for text extraction.");
    }

    private static string DecodeText(byte[] content)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private static string ExtractDocxText(byte[] content)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        var entry = archive.GetEntry("word/document.xml")
                    ?? throw new InvalidOperationException("The DOCX file does not contain the main document payload.");

        using var entryStream = entry.Open();
        var document = XDocument.Load(entryStream);
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        var paragraphs = document
            .Descendants(w + "p")
            .Select(p => string.Concat(
                p.Descendants(w + "t")
                    .Select(t => t.Value)))
            .Where(x => !string.IsNullOrWhiteSpace(x));

        return string.Join(Environment.NewLine, paragraphs);
    }

    private static string ExtractPdfText(byte[] content)
    {
        using var document = PdfDocument.Open(content);
        var pages = document
            .GetPages()
            .Select(page => ContentOrderTextExtractor.GetText(page))
            .Where(x => !string.IsNullOrWhiteSpace(x));

        return string.Join(Environment.NewLine, pages);
    }
}

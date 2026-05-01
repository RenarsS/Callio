using Callio.Knowledge.Application.KnowledgeDocuments;
using Callio.Knowledge.Domain;
using System.Security.Cryptography;
using System.Text;

namespace Callio.Knowledge.Infrastructure.Services.KnowledgeDocuments;

public class TenantKnowledgeFileMetadataFactory : ITenantKnowledgeFileMetadataFactory
{
    public TenantKnowledgeFileMetadata Create(
        UploadTenantKnowledgeDocumentCommand command,
        TenantKnowledgeCategory? category,
        IReadOnlyList<TenantKnowledgeTag> tags)
    {
        var title = ResolveTitle(command.Title, command.FileName);
        var contentType = string.IsNullOrWhiteSpace(command.ContentType)
            ? "application/octet-stream"
            : command.ContentType.Trim();
        var fileExtension = Path.GetExtension(command.FileName)?.Trim().ToLowerInvariant() ?? string.Empty;

        var blobMetadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["tenantid"] = command.TenantId.ToString(),
            ["source"] = command.SourceType.ToString(),
            ["title"] = title,
            ["category"] = category?.Name ?? string.Empty,
            ["tags"] = string.Join(',', tags.Select(x => x.Name))
        };

        return new TenantKnowledgeFileMetadata(
            title,
            contentType,
            fileExtension,
            ComputeSha256(command.Content),
            blobMetadata);
    }

    private static string ResolveTitle(string? title, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(title))
            return title.Trim();

        var inferred = Path.GetFileNameWithoutExtension(fileName)?.Trim();
        if (!string.IsNullOrWhiteSpace(inferred))
            return inferred;

        return Path.GetFileName(fileName);
    }

    private static string ComputeSha256(byte[] content)
    {
        var hash = SHA256.HashData(content);
        var builder = new StringBuilder(hash.Length * 2);
        foreach (var value in hash)
        {
            builder.Append(value.ToString("x2"));
        }

        return builder.ToString();
    }
}

using Callio.Provisioning.Domain;
using Callio.Provisioning.Domain.Enums;
using FluentAssertions;

namespace Callio.Provisioning.Tests.Domain;

public class TenantKnowledgeDocumentTests
{
    [Fact]
    public void Constructor_InitializesMetadataAndNormalizesFileExtension()
    {
        var now = new DateTime(2026, 4, 6, 18, 0, 0, DateTimeKind.Utc);

        var document = CreateDocument(now);

        document.DocumentKey.Should().NotBe(Guid.Empty);
        document.FileExtension.Should().Be(".pdf");
        document.ProcessingStatus.Should().Be(KnowledgeDocumentProcessingStatus.Pending);
        document.CreatedAtUtc.Should().Be(now);
        document.UpdatedAtUtc.Should().Be(now);
    }

    [Fact]
    public void AssignTags_DeduplicatesTagIdentifiers()
    {
        var document = CreateDocument(DateTime.UtcNow);

        document.AssignTags([1, 2, 2, 3], DateTime.UtcNow);

        document.DocumentTags.Should().HaveCount(3);
        document.DocumentTags.Select(x => x.TenantKnowledgeTagId).Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public void MarkReady_AttachesChunksAndUpdatesStatus()
    {
        var document = CreateDocument(DateTime.UtcNow);
        var completedAt = new DateTime(2026, 4, 6, 18, 30, 0, DateTimeKind.Utc);
        var chunks = new[]
        {
            new TenantKnowledgeDocumentChunk(0, "tenant-42:doc:0000", "tenant-42", "text-embedding-3-small", 4, "chunk-one", "[0.1,0.2,0.3,0.4]", completedAt),
            new TenantKnowledgeDocumentChunk(1, "tenant-42:doc:0001", "tenant-42", "text-embedding-3-small", 4, "chunk-two", "[0.2,0.3,0.4,0.5]", completedAt)
        };

        document.MarkReady(chunks, completedAt);

        document.ProcessingStatus.Should().Be(KnowledgeDocumentProcessingStatus.Ready);
        document.ChunkCount.Should().Be(2);
        document.IndexedAtUtc.Should().Be(completedAt);
        document.Chunks.Should().HaveCount(2);
    }

    [Fact]
    public void MarkFailed_ClearsIndexedChunksAndStoresError()
    {
        var document = CreateDocument(DateTime.UtcNow);
        document.MarkReady(
            [new TenantKnowledgeDocumentChunk(0, "tenant-42:doc:0000", "tenant-42", "text-embedding-3-small", 4, "chunk-one", "[0.1,0.2,0.3,0.4]", DateTime.UtcNow)],
            DateTime.UtcNow);

        var failedAt = new DateTime(2026, 4, 6, 19, 0, 0, DateTimeKind.Utc);
        document.MarkFailed("Embedding provider unavailable.", failedAt);

        document.ProcessingStatus.Should().Be(KnowledgeDocumentProcessingStatus.Failed);
        document.ChunkCount.Should().Be(0);
        document.Chunks.Should().BeEmpty();
        document.IndexedAtUtc.Should().BeNull();
        document.LastError.Should().Be("Embedding provider unavailable.");
        document.UpdatedAtUtc.Should().Be(failedAt);
    }

    [Fact]
    public void Category_And_Tag_NormalizeWhitespaceAndLookupKeys()
    {
        var now = new DateTime(2026, 4, 6, 20, 0, 0, DateTimeKind.Utc);

        var category = new TenantKnowledgeCategory(42, "  Product   Docs  ", "  Internal   only ", now);
        var tag = new TenantKnowledgeTag(42, "  Release   Notes  ", now);

        category.Name.Should().Be("Product Docs");
        category.NormalizedName.Should().Be("PRODUCT DOCS");
        category.Description.Should().Be("Internal only");
        tag.Name.Should().Be("Release Notes");
        tag.NormalizedName.Should().Be("RELEASE NOTES");
    }

    private static TenantKnowledgeDocument CreateDocument(DateTime now)
        => new(
            42,
            5,
            3,
            "Tenant Handbook",
            "Handbook.PDF",
            "application/pdf",
            "PDF",
            4096,
            "tenant-knowledge",
            "42/handbook.pdf",
            "https://storage.example/tenant-knowledge/42/handbook.pdf",
            "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            "tenant-42",
            KnowledgeDocumentSourceType.ManualUpload,
            "user-123",
            "Portal User",
            now);
}

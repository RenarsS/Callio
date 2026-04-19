using Callio.Provisioning.Domain;
using Callio.Provisioning.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Callio.Provisioning.Infrastructure.Persistence;

public class TenantKnowledgeCategoryEntityConfiguration(string schemaName)
    : IEntityTypeConfiguration<TenantKnowledgeCategory>
{
    public void Configure(EntityTypeBuilder<TenantKnowledgeCategory> builder)
    {
        builder.ToTable(TenantKnowledgeDocumentDbContext.CategoriesTableName, schemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasIndex(x => x.NormalizedName).IsUnique();
    }
}

public class TenantKnowledgeTagEntityConfiguration(string schemaName)
    : IEntityTypeConfiguration<TenantKnowledgeTag>
{
    public void Configure(EntityTypeBuilder<TenantKnowledgeTag> builder)
    {
        builder.ToTable(TenantKnowledgeDocumentDbContext.TagsTableName, schemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(80).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasIndex(x => x.NormalizedName).IsUnique();
    }
}

public class TenantKnowledgeDocumentEntityConfiguration(string schemaName)
    : IEntityTypeConfiguration<TenantKnowledgeDocument>
{
    public void Configure(EntityTypeBuilder<TenantKnowledgeDocument> builder)
    {
        builder.ToTable(TenantKnowledgeDocumentDbContext.DocumentsTableName, schemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.DocumentKey).IsRequired();
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.KnowledgeConfigurationId).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.OriginalFileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(256).IsRequired();
        builder.Property(x => x.FileExtension).HasMaxLength(32).IsRequired();
        builder.Property(x => x.SizeBytes).IsRequired();
        builder.Property(x => x.BlobContainerName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.BlobName).HasMaxLength(512).IsRequired();
        builder.Property(x => x.BlobUri).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.VectorStoreNamespace).HasMaxLength(256).IsRequired();
        builder.Property(x => x.SourceType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ProcessingStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.UploadedByUserId).HasMaxLength(128);
        builder.Property(x => x.UploadedByDisplayName).HasMaxLength(200);
        builder.Property(x => x.LastError).HasMaxLength(4000);
        builder.Property(x => x.ChunkCount).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.IndexedAtUtc);

        builder.HasIndex(x => x.DocumentKey).IsUnique();
        builder.HasIndex(x => x.ProcessingStatus);
        builder.HasIndex(x => x.CategoryId);

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class TenantKnowledgeDocumentTagEntityConfiguration(string schemaName)
    : IEntityTypeConfiguration<TenantKnowledgeDocumentTag>
{
    public void Configure(EntityTypeBuilder<TenantKnowledgeDocumentTag> builder)
    {
        builder.ToTable(TenantKnowledgeDocumentDbContext.DocumentTagsTableName, schemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.TenantKnowledgeDocumentId, x.TenantKnowledgeTagId }).IsUnique();

        builder.HasOne(x => x.Document)
            .WithMany(x => x.DocumentTags)
            .HasForeignKey(x => x.TenantKnowledgeDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Tag)
            .WithMany(x => x.DocumentTags)
            .HasForeignKey(x => x.TenantKnowledgeTagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TenantKnowledgeDocumentChunkEntityConfiguration(string schemaName)
    : IEntityTypeConfiguration<TenantKnowledgeDocumentChunk>
{
    public void Configure(EntityTypeBuilder<TenantKnowledgeDocumentChunk> builder)
    {
        builder.ToTable(TenantKnowledgeDocumentDbContext.DocumentChunksTableName, schemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.ChunkIndex).IsRequired();
        builder.Property(x => x.VectorRecordId).HasMaxLength(256).IsRequired();
        builder.Property(x => x.VectorStoreNamespace).HasMaxLength(256).IsRequired();
        builder.Property(x => x.EmbeddingModel).HasMaxLength(256).IsRequired();
        builder.Property(x => x.EmbeddingDimensions).IsRequired();
        builder.Property(x => x.Content).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.CharacterCount).IsRequired();
        builder.Property(x => x.TokenCountEstimate).IsRequired();
        builder.Property(x => x.EmbeddingJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.TenantKnowledgeDocumentId, x.ChunkIndex }).IsUnique();
        builder.HasIndex(x => x.VectorRecordId).IsUnique();

        builder.HasOne(x => x.Document)
            .WithMany(x => x.Chunks)
            .HasForeignKey(x => x.TenantKnowledgeDocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

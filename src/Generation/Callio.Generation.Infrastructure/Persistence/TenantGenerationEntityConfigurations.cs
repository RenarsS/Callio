using Callio.Generation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Callio.Generation.Infrastructure.Persistence;

public class TenantGenerationResponseEntityConfiguration(string schemaName)
    : IEntityTypeConfiguration<TenantGenerationResponse>
{
    public void Configure(EntityTypeBuilder<TenantGenerationResponse> builder)
    {
        builder.ToTable(TenantGenerationDbContext.ResponsesTableName, schemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.ResponseKey).IsRequired();
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.PromptKey).HasMaxLength(120).IsRequired();
        builder.Property(x => x.PromptName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Input).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.SystemPrompt).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.UserPrompt).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.FinalPrompt).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.ResponseText).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.GenerationModel).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(4000);
        builder.Property(x => x.RequestedByUserId).HasMaxLength(128);
        builder.Property(x => x.RequestedByDisplayName).HasMaxLength(200);
        builder.Property(x => x.SourceCount).IsRequired();
        builder.Property(x => x.EstimatedInputTokens).IsRequired();
        builder.Property(x => x.EstimatedOutputTokens).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CompletedAtUtc);

        builder.HasIndex(x => x.ResponseKey).IsUnique();
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}

public class TenantGenerationResponseSourceEntityConfiguration(string schemaName)
    : IEntityTypeConfiguration<TenantGenerationResponseSource>
{
    public void Configure(EntityTypeBuilder<TenantGenerationResponseSource> builder)
    {
        builder.ToTable(TenantGenerationDbContext.ResponseSourcesTableName, schemaName);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.SourceKind).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.DocumentTitle).HasMaxLength(256);
        builder.Property(x => x.CategoryName).HasMaxLength(120);
        builder.Property(x => x.Score).HasColumnType("decimal(18,6)");
        builder.Property(x => x.BlobContainerName).HasMaxLength(128);
        builder.Property(x => x.BlobName).HasMaxLength(512);
        builder.Property(x => x.BlobUri).HasMaxLength(2000);
        builder.Property(x => x.ContentExcerpt).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => x.KnowledgeDocumentId);
        builder.HasIndex(x => x.CategoryId);
        builder.HasIndex(x => x.ChunkId);

        builder.HasOne(x => x.Response)
            .WithMany(x => x.Sources)
            .HasForeignKey(x => x.TenantGenerationResponseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using Callio.Knowledge.Domain;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Callio.Knowledge.Infrastructure.Persistence;

public class TenantKnowledgeConfigurationEntityConfiguration
    : IEntityTypeConfiguration<TenantKnowledgeConfiguration>
{
    private static readonly ValueConverter<List<string>, string> AllowedFileTypesConverter = new(
        value => string.Join(';', value),
        value => value
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList());

    private static readonly ValueComparer<List<string>> AllowedFileTypesComparer = new(
        (left, right) => left == null && right == null
            || left != null
            && right != null
            && left.SequenceEqual(right, StringComparer.OrdinalIgnoreCase),
        value => value == null
            ? 0
            : value.Aggregate(
                0,
                (hash, item) => HashCode.Combine(hash, StringComparer.OrdinalIgnoreCase.GetHashCode(item))),
        value => value == null
            ? new List<string>()
            : value.ToList());

    public void Configure(EntityTypeBuilder<TenantKnowledgeConfiguration> builder)
    {
        builder.ToTable(TenantKnowledgeConfigurationDbContext.TableName);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.SystemPrompt).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(x => x.AssistantInstructionPrompt).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(x => x.ChunkSize).IsRequired();
        builder.Property(x => x.ChunkOverlap).IsRequired();
        builder.Property(x => x.TopKRetrievalCount).IsRequired();
        builder.Property(x => x.MaximumChunksInFinalContext).IsRequired();
        builder.Property(x => x.MinimumSimilarityThreshold).HasPrecision(5, 4).IsRequired();
        builder.Property(x => x.AllowedFileTypes)
            .HasConversion(AllowedFileTypesConverter)
            .HasMaxLength(1000)
            .IsRequired();
        builder.Property(x => x.AllowedFileTypes).Metadata.SetValueComparer(AllowedFileTypesComparer);
        builder.Property(x => x.MaximumFileSizeBytes).IsRequired();
        builder.Property(x => x.AutoProcessOnUpload).IsRequired();
        builder.Property(x => x.ManualApprovalRequiredBeforeIndexing).IsRequired();
        builder.Property(x => x.VersioningEnabled).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasIndex(x => x.IsActive)
            .HasFilter("[IsActive] = 1")
            .IsUnique();
    }
}

using Callio.Knowledge.Domain;
using Microsoft.EntityFrameworkCore;

namespace Callio.Knowledge.Infrastructure.Persistence;

public class TenantKnowledgeDocumentDbContext(
    DbContextOptions<TenantKnowledgeDocumentDbContext> options,
    string schemaName) : DbContext(options)
{
    public const string CategoriesTableName = "KnowledgeCategories";
    public const string TagsTableName = "KnowledgeTags";
    public const string DocumentsTableName = "KnowledgeDocuments";
    public const string DocumentTagsTableName = "KnowledgeDocumentTags";
    public const string DocumentChunksTableName = "KnowledgeDocumentChunks";

    public string SchemaName { get; } = string.IsNullOrWhiteSpace(schemaName)
        ? throw new ArgumentException("Schema name is required.", nameof(schemaName))
        : schemaName.Trim();

    public DbSet<TenantKnowledgeCategory> Categories => Set<TenantKnowledgeCategory>();

    public DbSet<TenantKnowledgeTag> Tags => Set<TenantKnowledgeTag>();

    public DbSet<TenantKnowledgeDocument> Documents => Set<TenantKnowledgeDocument>();

    public DbSet<TenantKnowledgeDocumentTag> DocumentTags => Set<TenantKnowledgeDocumentTag>();

    public DbSet<TenantKnowledgeDocumentChunk> DocumentChunks => Set<TenantKnowledgeDocumentChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new TenantKnowledgeCategoryEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TenantKnowledgeTagEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TenantKnowledgeDocumentEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TenantKnowledgeDocumentTagEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TenantKnowledgeDocumentChunkEntityConfiguration());
    }
}

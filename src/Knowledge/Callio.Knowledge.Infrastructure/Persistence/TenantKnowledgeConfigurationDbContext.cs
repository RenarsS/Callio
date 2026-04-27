using Callio.Knowledge.Domain;
using Microsoft.EntityFrameworkCore;

namespace Callio.Knowledge.Infrastructure.Persistence;

public class TenantKnowledgeConfigurationDbContext(
    DbContextOptions<TenantKnowledgeConfigurationDbContext> options,
    string schemaName) : DbContext(options)
{
    public const string TableName = "KnowledgeConfigurations";

    public string SchemaName { get; } = string.IsNullOrWhiteSpace(schemaName)
        ? throw new ArgumentException("Schema name is required.", nameof(schemaName))
        : schemaName.Trim();

    public DbSet<TenantKnowledgeConfiguration> KnowledgeConfigurations => Set<TenantKnowledgeConfiguration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new TenantKnowledgeConfigurationEntityConfiguration());
    }
}

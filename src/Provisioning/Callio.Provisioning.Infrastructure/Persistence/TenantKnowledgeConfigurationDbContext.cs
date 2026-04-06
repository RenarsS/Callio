using Callio.Provisioning.Domain;
using Microsoft.EntityFrameworkCore;

namespace Callio.Provisioning.Infrastructure.Persistence;

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
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new TenantKnowledgeConfigurationEntityConfiguration(SchemaName));
    }
}

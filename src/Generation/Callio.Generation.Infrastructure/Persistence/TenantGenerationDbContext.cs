using Callio.Generation.Domain;
using Microsoft.EntityFrameworkCore;

namespace Callio.Generation.Infrastructure.Persistence;

public class TenantGenerationDbContext(
    DbContextOptions<TenantGenerationDbContext> options,
    string schemaName) : DbContext(options)
{
    public const string ResponsesTableName = "GenerationResponses";
    public const string ResponseSourcesTableName = "GenerationResponseSources";

    public string SchemaName { get; } = string.IsNullOrWhiteSpace(schemaName)
        ? throw new ArgumentException("Schema name is required.", nameof(schemaName))
        : schemaName.Trim();

    public DbSet<TenantGenerationResponse> Responses => Set<TenantGenerationResponse>();

    public DbSet<TenantGenerationResponseSource> ResponseSources => Set<TenantGenerationResponseSource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new TenantGenerationResponseEntityConfiguration(SchemaName));
        modelBuilder.ApplyConfiguration(new TenantGenerationResponseSourceEntityConfiguration(SchemaName));
    }
}

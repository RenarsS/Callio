using Callio.Knowledge.Domain;
using Microsoft.EntityFrameworkCore;

namespace Callio.Knowledge.Infrastructure.Persistence;

public class KnowledgeDbContext(DbContextOptions<KnowledgeDbContext> options) : DbContext(options)
{
    public DbSet<TenantKnowledgeConfigurationSetup> TenantKnowledgeConfigurationSetups { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("provisioning");

        modelBuilder.Entity<TenantKnowledgeConfigurationSetup>(builder =>
        {
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            builder.Property(x => x.LastError).HasMaxLength(4000);

            builder.HasIndex(x => x.TenantId).IsUnique();
        });
    }
}

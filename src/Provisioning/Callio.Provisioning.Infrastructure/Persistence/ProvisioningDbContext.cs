using Callio.Provisioning.Domain;
using Microsoft.EntityFrameworkCore;

namespace Callio.Provisioning.Infrastructure.Persistence;

public class ProvisioningDbContext(DbContextOptions<ProvisioningDbContext> options) : DbContext(options)
{
    public DbSet<TenantInfrastructureProvisioning> TenantInfrastructureProvisionings { get; set; }

    public DbSet<TenantInfrastructureProvisioningStep> TenantInfrastructureProvisioningSteps { get; set; }

    public DbSet<TenantKnowledgeBaseSettings> TenantKnowledgeBaseSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("provisioning");

        modelBuilder.Entity<TenantInfrastructureProvisioning>(builder =>
        {
            builder.Property(x => x.RequestedByUserId).HasMaxLength(128);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            builder.Property(x => x.DatabaseSchema).HasMaxLength(128);
            builder.Property(x => x.VectorStoreNamespace).HasMaxLength(256);
            builder.Property(x => x.FailedStep).HasMaxLength(64);
            builder.Property(x => x.LastError).HasMaxLength(4000);

            builder.HasIndex(x => x.TenantId).IsUnique();
            builder.HasMany(x => x.Steps)
                .WithOne()
                .HasForeignKey(x => x.TenantInfrastructureProvisioningId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TenantInfrastructureProvisioningStep>(builder =>
        {
            builder.Property(x => x.Name).HasMaxLength(64);
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            builder.Property(x => x.LastError).HasMaxLength(4000);

            builder.HasIndex(x => new { x.TenantInfrastructureProvisioningId, x.Name }).IsUnique();
        });

        modelBuilder.Entity<TenantKnowledgeBaseSettings>(builder =>
        {
            builder.Property(x => x.DatabaseSchema).HasMaxLength(128);
            builder.Property(x => x.VectorStoreNamespace).HasMaxLength(256);
            builder.Property(x => x.EmbeddingProvider).HasMaxLength(100);
            builder.Property(x => x.EmbeddingModel).HasMaxLength(200);

            builder.HasIndex(x => x.TenantId).IsUnique();
        });
    }
}

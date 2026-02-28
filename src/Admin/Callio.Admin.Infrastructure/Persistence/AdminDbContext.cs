using Callio.Admin.Domain;
using Callio.Admin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Callio.Admin.Infrastructure.Persistence;

public class AdminDbContext : DbContext
{
    public DbSet<Tenant> Tenants { get; set; }
    
    public DbSet<Subscription> Subscriptions { get; set; }
    
    public DbSet<Plan> Plans { get; set; }

    public DbSet<PlanQuota> PlanQuotas { get; set; }

    public DbSet<UsageMetric> UsageMetrics { get; set; }

    public DbSet<UsageRecord> UsageRecords { get; set; }

    public DbSet<Invoice> Invoices { get; set; }

    public DbSet<InvoiceLineItem> InvoiceLineItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("admin");

        modelBuilder.Entity<Tenant>()
            .OwnsOne(t => t.Status, statusBuilder =>
            {
                statusBuilder.ToTable("TenancyStatus");
            })
            .OwnsOne(c => c.Contact, contactBuilder =>
            {
                contactBuilder.ToTable("Contact");
                contactBuilder.OwnsOne(c => c.Address, addressBuilder =>
                {
                    addressBuilder.ToTable("Address");
                });
            });
        
        base.OnModelCreating(modelBuilder);
    }
}
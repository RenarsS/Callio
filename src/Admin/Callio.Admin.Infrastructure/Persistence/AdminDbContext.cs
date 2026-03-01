using Callio.Admin.Domain;
using Callio.Admin.Domain.Enums;
using Callio.Admin.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Callio.Admin.Infrastructure.Persistence;

public class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options)
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
        base.OnModelCreating(modelBuilder);
        
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

        modelBuilder.Entity<Invoice>()
            .OwnsOne(i => i.BillingAddress, billingAddressBuilder =>
            {
                billingAddressBuilder.ToTable("BillingAddress");
            })
            .OwnsOne(i => i.BillingPeriod, billingPeriodBuilder =>
            {
                billingPeriodBuilder.Property(bp => bp.Start).HasColumnName("BillingPeriodStart");
                billingPeriodBuilder.Property(bp => bp.End).HasColumnName("BillingPeriodEnd");
                billingPeriodBuilder.Ignore(bp => bp.Now);
            })
            .OwnsOne(i => i.Subtotal, subtotalBuilder =>
            {
                subtotalBuilder.Property(subtotal => subtotal.Amount).HasColumnName("SubtotalAmount");
                subtotalBuilder.Ignore(subtotal => subtotal.Currency);
            })
            .OwnsOne(i => i.Tax, taxBuilder =>
            {
                taxBuilder.Property(tax => tax.Amount).HasColumnName("TaxAmount");
                taxBuilder.Ignore(tax => tax.Currency);
            })
            .OwnsOne(i => i.Total, totalBuilder =>
            {
                totalBuilder.Property(total => total.Amount).HasColumnName("TotalAmount");
                totalBuilder.Property(total => total.Currency).HasColumnName("Currency");
            });

        modelBuilder.Entity<InvoiceLineItem>()
            .OwnsOne(il => il.UnitPrice, unitPriceBuilder =>
            {
                unitPriceBuilder.Property(unitPrice => unitPrice.Amount).HasColumnName("UnitPriceAmount");
                unitPriceBuilder.Ignore(unitPrice => unitPrice.Currency);
            })
            .OwnsOne(il => il.Total, totalBuilder =>
            {
                totalBuilder.Property(total => total.Amount).HasColumnName("TotalAmount");
                totalBuilder.Property(total => total.Currency).HasColumnName("Currency");
            });

        modelBuilder.Entity<Subscription>()
            .OwnsOne(s => s.CurrentPeriod, periodBuilder =>
            {
                periodBuilder.Property(p => p.Start).HasColumnName("CurrentPeriodStart");
                periodBuilder.Property(p => p.End).HasColumnName("CurrentPeriodEnd");
                periodBuilder.Ignore(p => p.Now);
            });

        modelBuilder.Entity<Plan>()
            .OwnsOne(p => p.BillingCycle, billingCycleBuilder =>
            {
                billingCycleBuilder.Property(bc => bc.AnchorDay);
                billingCycleBuilder.Property(bc => bc.Interval);
            })
            .OwnsOne(p => p.BasePrice, basePriceBuilder =>
            {
                basePriceBuilder.Property(basePrice => basePrice.Amount).HasColumnName("BasePriceAmount");
                basePriceBuilder.Property(basePrice => basePrice.Currency).HasColumnName("Currency");
            });

        modelBuilder.Entity<PlanQuota>()
            .OwnsOne(pq => pq.OverageUnitPrice, overageUnitPriceBuilder =>
            {
                overageUnitPriceBuilder.Property(overageUnit => overageUnit.Amount).HasColumnName("OverageUnitPriceAmount");
                overageUnitPriceBuilder.Property(overageUnit => overageUnit.Currency).HasColumnName("Currency");
            });
    }
}
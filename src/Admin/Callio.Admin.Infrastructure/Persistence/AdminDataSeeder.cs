using Callio.Admin.Domain;
using Callio.Admin.Domain.Enums;
using Callio.Admin.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Callio.Admin.Infrastructure.Persistence;

public static class AdminDataSeeder
{
    public static async Task SeedAsync(AdminDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        if (!await dbContext.UsageMetrics.AnyAsync(cancellationToken))
        {
            dbContext.UsageMetrics.AddRange(
                new UsageMetric("documents", "Indexed documents", "docs", MeasurementType.Cumulative),
                new UsageMetric("storage_gb", "Vector storage", "GB", MeasurementType.Snapshot),
                new UsageMetric("rag_queries", "RAG queries", "queries", MeasurementType.Cumulative),
                new UsageMetric("ingestion_jobs", "Ingestion jobs", "jobs", MeasurementType.Cumulative));

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.Plans.AnyAsync(cancellationToken))
        {
            var starter = new Plan("Starter", "A light plan for early validation and internal pilots.", new Money(49, "EUR"), new BillingCycle(BillingInterval.Monthly, 1));
            var growth = new Plan("Growth", "Balanced plan for teams with regular ingestion and RAG usage.", new Money(199, "EUR"), new BillingCycle(BillingInterval.Monthly, 1));
            var enterprise = new Plan("Enterprise", "Custom scale with generous quotas and softer overages.", new Money(799, "EUR"), new BillingCycle(BillingInterval.Monthly, 1));

            dbContext.Plans.AddRange(starter, growth, enterprise);
            await dbContext.SaveChangesAsync(cancellationToken);

            var metrics = await dbContext.UsageMetrics.OrderBy(x => x.Id).ToListAsync(cancellationToken);
            var documents = metrics.First(x => x.Key == "documents");
            var storage = metrics.First(x => x.Key == "storage_gb");
            var ragQueries = metrics.First(x => x.Key == "rag_queries");
            var ingestionJobs = metrics.First(x => x.Key == "ingestion_jobs");

            dbContext.PlanQuotas.AddRange(
                new PlanQuota(starter.Id, documents.Id, 10000, true),
                new PlanQuota(starter.Id, storage.Id, 5, true),
                new PlanQuota(starter.Id, ragQueries.Id, 25000, false, new Money(0.002m, "EUR")),
                new PlanQuota(starter.Id, ingestionJobs.Id, 200, false, new Money(0.10m, "EUR")),

                new PlanQuota(growth.Id, documents.Id, 100000, true),
                new PlanQuota(growth.Id, storage.Id, 50, true),
                new PlanQuota(growth.Id, ragQueries.Id, 250000, false, new Money(0.0012m, "EUR")),
                new PlanQuota(growth.Id, ingestionJobs.Id, 2000, false, new Money(0.05m, "EUR")),

                new PlanQuota(enterprise.Id, documents.Id, -1, false),
                new PlanQuota(enterprise.Id, storage.Id, 250, false, new Money(0.5m, "EUR")),
                new PlanQuota(enterprise.Id, ragQueries.Id, -1, false),
                new PlanQuota(enterprise.Id, ingestionJobs.Id, 10000, false, new Money(0.02m, "EUR")));

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

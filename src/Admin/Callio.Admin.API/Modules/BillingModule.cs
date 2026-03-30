using Carter;
using Callio.Admin.API.Contracts.Billing;
using Callio.Admin.Domain;
using Callio.Admin.Domain.ValueObjects;
using Callio.Admin.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Callio.Admin.API.Modules;

public class BillingModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var dashboard = app.MapGroup("api/dashboard").WithTags("Dashboard Billing");
        var portal = app.MapGroup("api/portal").WithTags("Portal Plans");

        dashboard.MapGet("/plans", async (AdminDbContext db, CancellationToken ct) =>
        {
            var plans = await db.Plans
                .AsNoTracking()
                .OrderBy(x => x.BasePrice.Amount)
                .ToListAsync(ct);

            var quotaRows = await db.PlanQuotas
                .AsNoTracking()
                .Join(db.UsageMetrics.AsNoTracking(),
                    q => q.UsageMetricId,
                    m => m.Id,
                    (q, m) => new
                    {
                        q.PlanId,
                        q.Id,
                        q.UsageMetricId,
                        MetricKey = m.Key,
                        MetricDisplayName = m.DisplayName,
                        MetricUnit = m.Unit,
                        q.Limit,
                        q.HardLimit,
                        OverageAmount = q.OverageUnitPrice != null ? q.OverageUnitPrice.Amount : (decimal?)null,
                        OverageCurrency = q.OverageUnitPrice != null ? q.OverageUnitPrice.Currency : null
                    })
                .ToListAsync(ct);

            var quotaLookup = quotaRows
                .GroupBy(x => x.PlanId)
                .ToDictionary(
                    x => x.Key,
                    x => (IReadOnlyList<PlanQuotaResponse>)x
                        .OrderBy(y => y.MetricDisplayName)
                        .Select(y => new PlanQuotaResponse(
                            y.Id,
                            y.UsageMetricId,
                            y.MetricKey,
                            y.MetricDisplayName,
                            y.MetricUnit,
                            y.Limit,
                            y.HardLimit,
                            y.OverageAmount,
                            y.OverageCurrency))
                        .ToList());

            var response = plans
                .Select(x => new PlanListItemResponse(
                    x.Id,
                    x.Name,
                    x.Description,
                    x.BasePrice.Amount,
                    x.BasePrice.Currency,
                    x.BillingCycle.Interval,
                    x.BillingCycle.AnchorDay,
                    x.IsActive,
                    quotaLookup.GetValueOrDefault(x.Id, [])))
                .ToList();

            return Results.Ok(response);
        });

        dashboard.MapPost("/plans", async (CreatePlanRequest request, AdminDbContext db, CancellationToken ct) =>
        {
            var plan = new Plan(
                request.Name.Trim(),
                request.Description.Trim(),
                new Money(request.BasePrice, request.Currency.Trim()),
                new BillingCycle(request.BillingInterval, request.AnchorDay));

            if (!request.IsActive)
                plan.Deactivate();

            db.Plans.Add(plan);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/dashboard/plans/{plan.Id}", plan.Id);
        });

        dashboard.MapPut("/plans/{planId:int}", async (int planId, UpdatePlanRequest request, AdminDbContext db, CancellationToken ct) =>
        {
            var plan = await db.Plans.FirstOrDefaultAsync(x => x.Id == planId, ct);
            if (plan is null) return Results.NotFound();

            typeof(Plan).GetProperty(nameof(Plan.Name))!.SetValue(plan, request.Name.Trim());
            typeof(Plan).GetProperty(nameof(Plan.Description))!.SetValue(plan, request.Description.Trim());
            typeof(Plan).GetProperty(nameof(Plan.BasePrice))!.SetValue(plan, new Money(request.BasePrice, request.Currency.Trim()));
            typeof(Plan).GetProperty(nameof(Plan.BillingCycle))!.SetValue(plan, new BillingCycle(request.BillingInterval, request.AnchorDay));
            typeof(Plan).GetProperty(nameof(Plan.IsActive))!.SetValue(plan, request.IsActive);

            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        dashboard.MapDelete("/plans/{planId:int}", async (int planId, AdminDbContext db, CancellationToken ct) =>
        {
            var plan = await db.Plans.FirstOrDefaultAsync(x => x.Id == planId, ct);
            if (plan is null) return Results.NotFound();

            var hasSubscriptions = await db.Subscriptions.AnyAsync(x => x.PlanId == planId, ct);
            if (hasSubscriptions)
                return Results.BadRequest("Plan is already assigned to tenants and cannot be deleted.");

            var quotas = await db.PlanQuotas.Where(x => x.PlanId == planId).ToListAsync(ct);
            db.PlanQuotas.RemoveRange(quotas);
            db.Plans.Remove(plan);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        dashboard.MapPost("/plans/{planId:int}/quotas", async (int planId, CreatePlanQuotaRequest request, AdminDbContext db, CancellationToken ct) =>
        {
            if (!await db.Plans.AnyAsync(x => x.Id == planId, ct)) return Results.NotFound("Plan was not found.");
            if (!await db.UsageMetrics.AnyAsync(x => x.Id == request.UsageMetricId, ct)) return Results.BadRequest("Usage metric was not found.");
            if (await db.PlanQuotas.AnyAsync(x => x.PlanId == planId && x.UsageMetricId == request.UsageMetricId, ct))
                return Results.BadRequest("This metric already has a quota for the selected plan.");

            var quota = new PlanQuota(
                planId,
                request.UsageMetricId,
                request.Limit,
                request.HardLimit,
                request.OverageUnitPrice.HasValue && !string.IsNullOrWhiteSpace(request.Currency)
                    ? new Money(request.OverageUnitPrice.Value, request.Currency!)
                    : null);

            db.PlanQuotas.Add(quota);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/dashboard/plans/{planId}/quotas/{quota.Id}", quota.Id);
        });

        dashboard.MapPut("/plans/{planId:int}/quotas/{quotaId:int}", async (int planId, int quotaId, UpdatePlanQuotaRequest request, AdminDbContext db, CancellationToken ct) =>
        {
            var quota = await db.PlanQuotas.FirstOrDefaultAsync(x => x.Id == quotaId && x.PlanId == planId, ct);
            if (quota is null) return Results.NotFound();

            typeof(PlanQuota).GetProperty(nameof(PlanQuota.Limit))!.SetValue(quota, request.Limit);
            typeof(PlanQuota).GetProperty(nameof(PlanQuota.HardLimit))!.SetValue(quota, request.HardLimit);
            typeof(PlanQuota).GetProperty(nameof(PlanQuota.OverageUnitPrice))!.SetValue(quota,
                request.OverageUnitPrice.HasValue && !string.IsNullOrWhiteSpace(request.Currency)
                    ? new Money(request.OverageUnitPrice.Value, request.Currency!)
                    : null);

            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        dashboard.MapDelete("/plans/{planId:int}/quotas/{quotaId:int}", async (int planId, int quotaId, AdminDbContext db, CancellationToken ct) =>
        {
            var quota = await db.PlanQuotas.FirstOrDefaultAsync(x => x.Id == quotaId && x.PlanId == planId, ct);
            if (quota is null) return Results.NotFound();

            db.PlanQuotas.Remove(quota);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        dashboard.MapGet("/usage-metrics", async (AdminDbContext db, CancellationToken ct) =>
            Results.Ok(await db.UsageMetrics
                .AsNoTracking()
                .OrderBy(x => x.DisplayName)
                .Select(x => new UsageMetricResponse(x.Id, x.Key, x.DisplayName, x.Unit, x.Type))
                .ToListAsync(ct)));

        dashboard.MapPost("/usage-metrics", async (CreateUsageMetricRequest request, AdminDbContext db, CancellationToken ct) =>
        {
            var metric = new UsageMetric(request.Key.Trim(), request.DisplayName.Trim(), request.Unit.Trim(), request.Type);
            db.UsageMetrics.Add(metric);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/dashboard/usage-metrics/{metric.Id}", metric.Id);
        });

        dashboard.MapPut("/usage-metrics/{metricId:int}", async (int metricId, UpdateUsageMetricRequest request, AdminDbContext db, CancellationToken ct) =>
        {
            var metric = await db.UsageMetrics.FirstOrDefaultAsync(x => x.Id == metricId, ct);
            if (metric is null) return Results.NotFound();

            typeof(UsageMetric).GetProperty(nameof(UsageMetric.Key))!.SetValue(metric, request.Key.Trim());
            typeof(UsageMetric).GetProperty(nameof(UsageMetric.DisplayName))!.SetValue(metric, request.DisplayName.Trim());
            typeof(UsageMetric).GetProperty(nameof(UsageMetric.Unit))!.SetValue(metric, request.Unit.Trim());
            typeof(UsageMetric).GetProperty(nameof(UsageMetric.Type))!.SetValue(metric, request.Type);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        dashboard.MapDelete("/usage-metrics/{metricId:int}", async (int metricId, AdminDbContext db, CancellationToken ct) =>
        {
            var metric = await db.UsageMetrics.FirstOrDefaultAsync(x => x.Id == metricId, ct);
            if (metric is null) return Results.NotFound();
            if (await db.PlanQuotas.AnyAsync(x => x.UsageMetricId == metricId, ct))
                return Results.BadRequest("Usage metric is used by one or more plan quotas and cannot be deleted.");

            db.UsageMetrics.Remove(metric);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        portal.MapGet("/plans", async (AdminDbContext db, CancellationToken ct) =>
        {
            var plans = await db.Plans
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.BasePrice.Amount)
                .ToListAsync(ct);

            var quotaRows = await db.PlanQuotas
                .AsNoTracking()
                .Join(db.UsageMetrics.AsNoTracking(),
                    q => q.UsageMetricId,
                    m => m.Id,
                    (q, m) => new
                    {
                        q.PlanId,
                        Metric = m.DisplayName,
                        MetricUnit = m.Unit,
                        q.Limit,
                        q.HardLimit,
                        OverageAmount = q.OverageUnitPrice != null ? q.OverageUnitPrice.Amount : (decimal?)null,
                        OverageCurrency = q.OverageUnitPrice != null ? q.OverageUnitPrice.Currency : null
                    })
                .ToListAsync(ct);

            var quotaLookup = quotaRows
                .GroupBy(x => x.PlanId)
                .ToDictionary(
                    x => x.Key,
                    x => (IReadOnlyList<PortalPlanQuotaResponse>)x
                        .OrderBy(y => y.Metric)
                        .Select(y => new PortalPlanQuotaResponse(
                            y.Metric,
                            y.Limit == -1 ? "Unlimited" : $"{y.Limit} {y.MetricUnit}",
                            y.HardLimit,
                            y.OverageAmount.HasValue ? $"{y.OverageAmount.Value} {y.OverageCurrency}/{y.MetricUnit}" : null))
                        .ToList());

            var response = plans
                .Select(x => new PortalPlanResponse(
                    x.Id,
                    x.Name,
                    x.Description,
                    x.BasePrice.Amount,
                    x.BasePrice.Currency,
                    x.BillingCycle.Interval.ToString(),
                    quotaLookup.GetValueOrDefault(x.Id, [])))
                .ToList();

            return Results.Ok(response);
        });
    }
}

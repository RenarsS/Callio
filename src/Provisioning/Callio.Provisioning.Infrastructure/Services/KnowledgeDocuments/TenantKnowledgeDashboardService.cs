using Callio.Admin.Infrastructure.Persistence;
using Callio.Provisioning.Application.KnowledgeDocuments;
using Callio.Provisioning.Domain.Enums;
using Callio.Provisioning.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Callio.Provisioning.Infrastructure.Services.KnowledgeDocuments;

public class TenantKnowledgeDashboardService(
    AdminDbContext adminDbContext,
    ProvisioningDbContext provisioningDbContext,
    ITenantKnowledgeDocumentDbContextFactory dbContextFactory,
    ITenantResourceNamingStrategy tenantResourceNamingStrategy,
    ILogger<TenantKnowledgeDashboardService> logger) : ITenantKnowledgeDashboardService
{
    public async Task<TenantKnowledgeDashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await adminDbContext.Tenants
            .AsNoTracking()
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (tenants.Count == 0)
            return new TenantKnowledgeDashboardOverviewDto(0, 0, 0, 0, 0, 0, 0);

        var schemaLookup = await provisioningDbContext.TenantInfrastructureProvisionings
            .AsNoTracking()
            .ToDictionaryAsync(x => x.TenantId, x => x.DatabaseSchema, cancellationToken);

        var tenantsWithDocuments = 0;
        var totalDocuments = 0;
        long totalStorageBytes = 0;
        var readyDocuments = 0;
        var failedDocuments = 0;
        var awaitingApprovalDocuments = 0;

        foreach (var tenantId in tenants)
        {
            var schemaName = schemaLookup.TryGetValue(tenantId, out var existingSchema)
                ? existingSchema
                : tenantResourceNamingStrategy.Create(tenantId).DatabaseSchema;

            try
            {
                await using var context = dbContextFactory.Create(schemaName);
                var aggregate = await context.Documents
                    .AsNoTracking()
                    .GroupBy(_ => 1)
                    .Select(group => new
                    {
                        TotalDocuments = group.Count(),
                        TotalStorageBytes = group.Sum(x => x.SizeBytes),
                        ReadyDocuments = group.Count(x => x.ProcessingStatus == KnowledgeDocumentProcessingStatus.Ready),
                        FailedDocuments = group.Count(x => x.ProcessingStatus == KnowledgeDocumentProcessingStatus.Failed),
                        AwaitingApprovalDocuments = group.Count(x => x.ProcessingStatus == KnowledgeDocumentProcessingStatus.AwaitingApproval)
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (aggregate is null || aggregate.TotalDocuments == 0)
                    continue;

                tenantsWithDocuments++;
                totalDocuments += aggregate.TotalDocuments;
                totalStorageBytes += aggregate.TotalStorageBytes;
                readyDocuments += aggregate.ReadyDocuments;
                failedDocuments += aggregate.FailedDocuments;
                awaitingApprovalDocuments += aggregate.AwaitingApprovalDocuments;
            }
            catch (SqlException ex) when (IsMissingKnowledgeStore(ex))
            {
                logger.LogDebug(
                    ex,
                    "Tenant knowledge store is not available yet for tenant {TenantId}. Skipping dashboard aggregation for that tenant.",
                    tenantId);
            }
        }

        return new TenantKnowledgeDashboardOverviewDto(
            tenants.Count,
            tenantsWithDocuments,
            totalDocuments,
            totalStorageBytes,
            readyDocuments,
            failedDocuments,
            awaitingApprovalDocuments);
    }

    private static bool IsMissingKnowledgeStore(SqlException exception)
        => exception.Number == 208
           || exception.Message.Contains("KnowledgeDocuments", StringComparison.OrdinalIgnoreCase)
           || exception.Message.Contains("KnowledgeCategories", StringComparison.OrdinalIgnoreCase);
}

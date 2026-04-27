using Callio.Admin.Infrastructure.Persistence;
using Callio.Generation.Infrastructure.Provisioners;
using Callio.Knowledge.Infrastructure.Provisioners;
using Callio.Provisioning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Callio.Provisioning.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace Callio.DatabaseTool;

internal sealed class TenantSchemaMigrationRunner(
    AdminDbContext adminDbContext,
    ProvisioningDbContext provisioningDbContext,
    ITenantKnowledgeConfigurationStoreProvisioner tenantKnowledgeConfigurationStoreProvisioner,
    ITenantKnowledgeDocumentStoreProvisioner tenantKnowledgeDocumentStoreProvisioner,
    ITenantGenerationStoreProvisioner tenantGenerationStoreProvisioner,
    ITenantResourceNamingStrategy tenantResourceNamingStrategy,
    ILogger<TenantSchemaMigrationRunner> logger)
{
    public async Task<int> MigrateAllAsync(CancellationToken cancellationToken)
    {
        var tenantIds = await adminDbContext.Tenants
            .AsNoTracking()
            .Select(x => x.Id)
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        if (tenantIds.Count == 0)
        {
            logger.LogInformation("No tenants were found. Tenant schema migration was skipped.");
            return 0;
        }

        var provisionedSchemaLookup = await provisioningDbContext.TenantInfrastructureProvisionings
            .AsNoTracking()
            .Where(x => tenantIds.Contains(x.TenantId))
            .ToDictionaryAsync(x => x.TenantId, x => x.DatabaseSchema, cancellationToken);

        var schemaNames = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tenantId in tenantIds)
        {
            var schemaName = provisionedSchemaLookup.TryGetValue(tenantId, out var provisionedSchema)
                && !string.IsNullOrWhiteSpace(provisionedSchema)
                ? provisionedSchema.Trim()
                : tenantResourceNamingStrategy.Create(tenantId).DatabaseSchema;

            schemaNames.Add(schemaName);
            logger.LogInformation(
                "Mapped tenant {TenantId} to tenant schema '{SchemaName}' for migration.",
                tenantId,
                schemaName);
        }

        foreach (var schemaName in schemaNames)
        {
            logger.LogInformation("Ensuring tenant schema '{SchemaName}' is up to date.", schemaName);
            await tenantKnowledgeConfigurationStoreProvisioner.EnsureCreatedAsync(schemaName, cancellationToken);
            await tenantKnowledgeDocumentStoreProvisioner.EnsureCreatedAsync(schemaName, cancellationToken);
            await tenantGenerationStoreProvisioner.EnsureCreatedAsync(schemaName, cancellationToken);
        }

        return schemaNames.Count;
    }
}

using Callio.Knowledge.Application.KnowledgeConfigurations;
using Callio.Knowledge.Domain;
using Callio.Knowledge.Infrastructure.Persistence;
using Callio.Knowledge.Infrastructure.Provisioners;
using Callio.Provisioning.Infrastructure.Persistence;
using Callio.Provisioning.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Callio.Knowledge.Infrastructure.Repositories;

public class TenantKnowledgeConfigurationRepository(
    ProvisioningDbContext provisioningDbContext,
    ITenantResourceNamingStrategy tenantResourceNamingStrategy,
    ITenantKnowledgeConfigurationDbContextFactory dbContextFactory,
    ITenantKnowledgeConfigurationStoreProvisioner storeProvisioner) : ITenantKnowledgeConfigurationRepository
{
    public async Task<TenantKnowledgeConfiguration?> GetByIdAsync(int tenantId, int configurationId, CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(tenantId, cancellationToken);
        return await context.KnowledgeConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.Id == configurationId,
                cancellationToken);
    }

    public async Task<TenantKnowledgeConfiguration?> GetActiveAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(tenantId, cancellationToken);
        return await context.KnowledgeConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.IsActive,
                cancellationToken);
    }

    public async Task<TenantKnowledgeConfiguration?> GetLatestAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(tenantId, cancellationToken);
        return await context.KnowledgeConfigurations
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, TenantKnowledgeConfiguration>> GetActiveByTenantIdsAsync(
        IReadOnlyCollection<int> tenantIds,
        CancellationToken cancellationToken = default)
    {
        if (tenantIds.Count == 0)
            return new Dictionary<int, TenantKnowledgeConfiguration>();

        var results = new Dictionary<int, TenantKnowledgeConfiguration>();
        foreach (var tenantId in tenantIds.Distinct())
        {
            var configuration = await GetActiveAsync(tenantId, cancellationToken);
            if (configuration is not null)
                results[tenantId] = configuration;
        }

        return results;
    }

    public async Task<TenantKnowledgeConfiguration> AddAsync(TenantKnowledgeConfiguration configuration, CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(configuration.TenantId, cancellationToken);

        context.KnowledgeConfigurations.Add(configuration);
        await context.SaveChangesAsync(cancellationToken);

        return configuration;
    }

    public async Task<TenantKnowledgeConfiguration> UpdateAsync(TenantKnowledgeConfiguration configuration, CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(configuration.TenantId, cancellationToken);

        context.KnowledgeConfigurations.Update(configuration);
        await context.SaveChangesAsync(cancellationToken);

        return configuration;
    }

    public async Task<TenantKnowledgeConfiguration?> ActivateAsync(int tenantId, int configurationId, DateTime now, CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(tenantId, cancellationToken);

        var configurations = await context.KnowledgeConfigurations
            .Where(x => x.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var target = configurations.FirstOrDefault(x => x.Id == configurationId);
        if (target is null)
            return null;

        foreach (var configuration in configurations.Where(x => x.IsActive && x.Id != configurationId))
        {
            configuration.Deactivate(now);
        }

        target.Activate(now);

        await context.SaveChangesAsync(cancellationToken);
        return target;
    }

    public async Task<TenantKnowledgeConfiguration?> DeactivateAsync(int tenantId, int configurationId, DateTime now, CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(tenantId, cancellationToken);

        var target = await context.KnowledgeConfigurations
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.Id == configurationId,
                cancellationToken);

        if (target is null)
            return null;

        target.Deactivate(now);
        await context.SaveChangesAsync(cancellationToken);

        return target;
    }

    private async Task<TenantKnowledgeConfigurationDbContext> CreateContextAsync(int tenantId, CancellationToken cancellationToken)
    {
        var schemaName = await ResolveSchemaNameAsync(tenantId, cancellationToken);
        await storeProvisioner.EnsureCreatedAsync(schemaName, cancellationToken);
        return dbContextFactory.Create(schemaName);
    }

    private async Task<string> ResolveSchemaNameAsync(int tenantId, CancellationToken cancellationToken)
    {
        var existingSchema = await provisioningDbContext.TenantInfrastructureProvisionings
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.DatabaseSchema)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(existingSchema))
            return existingSchema;

        return tenantResourceNamingStrategy.Create(tenantId).DatabaseSchema;
    }
}

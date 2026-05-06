using Callio.Generation.Application.Generation;
using Callio.Generation.Domain;
using Callio.Generation.Infrastructure.Persistence;
using Callio.Generation.Infrastructure.Provisioners;
using Callio.Generation.Infrastructure.Provisioning;
using Microsoft.EntityFrameworkCore;

namespace Callio.Generation.Infrastructure.Repositories;

public class TenantGenerationRepository(
    ITenantGenerationProvisioningResourcesProvider provisioningResourcesProvider,
    ITenantGenerationDbContextFactory dbContextFactory,
    ITenantGenerationStoreProvisioner storeProvisioner) : ITenantGenerationRepository
{
    public async Task<IReadOnlyList<TenantGenerationPromptTemplate>> GetPromptTemplatesAsync(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(tenantId, cancellationToken);

        return await context.PromptTemplates
            .AsNoTracking()
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ThenBy(x => x.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantGenerationPromptTemplate?> GetPromptTemplateByIdAsync(
        int tenantId,
        int promptTemplateId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(tenantId, cancellationToken);

        return await context.PromptTemplates
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.Id == promptTemplateId,
                cancellationToken);
    }

    public async Task<TenantGenerationPromptTemplate?> GetPromptTemplateByKeyAsync(
        int tenantId,
        string promptKey,
        CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(tenantId, cancellationToken);

        return await context.PromptTemplates
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.Key == promptKey,
                cancellationToken);
    }

    public async Task<TenantGenerationPromptTemplate> AddPromptTemplateAsync(
        TenantGenerationPromptTemplate promptTemplate,
        CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(promptTemplate.TenantId, cancellationToken);
        context.PromptTemplates.Add(promptTemplate);
        await context.SaveChangesAsync(cancellationToken);
        return promptTemplate;
    }

    public async Task<TenantGenerationPromptTemplate> UpdatePromptTemplateAsync(
        TenantGenerationPromptTemplate promptTemplate,
        CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(promptTemplate.TenantId, cancellationToken);
        context.PromptTemplates.Update(promptTemplate);
        await context.SaveChangesAsync(cancellationToken);
        return promptTemplate;
    }

    public async Task<TenantGenerationResponse> AddAsync(
        TenantGenerationResponse response,
        CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(response.TenantId, cancellationToken);
        context.Responses.Add(response);
        await context.SaveChangesAsync(cancellationToken);

        return await context.Responses
            .AsNoTracking()
            .Include(x => x.Sources)
            .FirstAsync(x => x.Id == response.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<TenantGenerationResponse>> GetRecentAsync(
        int tenantId,
        int take,
        CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(tenantId, cancellationToken);

        return await context.Responses
            .AsNoTracking()
            .Include(x => x.Sources)
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantGenerationResponse?> GetByIdAsync(
        int tenantId,
        int responseId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await CreateContextAsync(tenantId, cancellationToken);

        return await context.Responses
            .AsNoTracking()
            .Include(x => x.Sources)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == responseId, cancellationToken);
    }

    private async Task<TenantGenerationDbContext> CreateContextAsync(int tenantId, CancellationToken cancellationToken)
    {
        var resources = await provisioningResourcesProvider.GetAsync(tenantId, cancellationToken);
        await storeProvisioner.EnsureCreatedAsync(resources.DatabaseSchema, cancellationToken);
        return dbContextFactory.Create(resources.DatabaseSchema);
    }
}

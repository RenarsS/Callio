using Callio.Knowledge.Application.KnowledgeConfigurations;
using Callio.Knowledge.Domain;
using Callio.Knowledge.Domain.Enums;
using Callio.Knowledge.Infrastructure.Persistence;
using Callio.Knowledge.Infrastructure.Provisioners;
using Callio.Provisioning.Domain.Enums;
using Callio.Provisioning.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Callio.Knowledge.Infrastructure.Services;

public class TenantKnowledgeConfigurationSetupService(
    KnowledgeDbContext knowledgeDbContext,
    ProvisioningDbContext provisioningDbContext,
    IKnowledgeMetadataStoreProvisioner knowledgeMetadataStoreProvisioner,
    ITenantKnowledgeConfigurationService tenantKnowledgeConfigurationService,
    ILogger<TenantKnowledgeConfigurationSetupService> logger) : ITenantKnowledgeConfigurationSetupService
{
    public async Task EnsurePendingAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        await knowledgeMetadataStoreProvisioner.EnsureCreatedAsync(cancellationToken);

        var setup = await knowledgeDbContext.TenantKnowledgeConfigurationSetups
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (setup is null)
        {
            knowledgeDbContext.TenantKnowledgeConfigurationSetups.Add(
                TenantKnowledgeConfigurationSetup.CreatePending(tenantId, DateTime.UtcNow));
            await knowledgeDbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        if (setup.Status == KnowledgeConfigurationSetupStatus.Succeeded)
            return;

        setup.RefreshPending(DateTime.UtcNow);
        await knowledgeDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TenantKnowledgeConfigurationSetupStatusDto> HandleProvisioningSucceededAsync(
        RunTenantKnowledgeConfigurationSetupCommand command,
        CancellationToken cancellationToken = default)
    {
        await knowledgeMetadataStoreProvisioner.EnsureCreatedAsync(cancellationToken);

        var setup = await GetOrCreateAsync(command.TenantId, cancellationToken);
        var activeConfiguration = await tenantKnowledgeConfigurationService.GetActiveAsync(command.TenantId, cancellationToken);

        if (setup.Status == KnowledgeConfigurationSetupStatus.Succeeded && activeConfiguration is not null)
        {
            if (setup.ActiveConfigurationId != activeConfiguration.Id)
            {
                setup.MarkSucceeded(activeConfiguration.Id, DateTime.UtcNow);
                await knowledgeDbContext.SaveChangesAsync(cancellationToken);
            }

            return setup.ToDto();
        }

        setup.BeginAttempt(DateTime.UtcNow);
        await knowledgeDbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var configuration = await tenantKnowledgeConfigurationService.CreateDefaultAsync(
                new CreateDefaultTenantKnowledgeConfigurationCommand(command.TenantId),
                cancellationToken);

            setup.MarkSucceeded(configuration.Id, DateTime.UtcNow);
            await knowledgeDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            setup.MarkFailed(ex.GetBaseException().Message, DateTime.UtcNow);
            await knowledgeDbContext.SaveChangesAsync(cancellationToken);

            logger.LogWarning(
                ex,
                "Tenant knowledge configuration setup failed for tenant {TenantId}.",
                command.TenantId);
        }

        return setup.ToDto();
    }

    public async Task<TenantKnowledgeConfigurationSetupStatusDto?> RetryAsync(
        RunTenantKnowledgeConfigurationSetupCommand command,
        CancellationToken cancellationToken = default)
    {
        await knowledgeMetadataStoreProvisioner.EnsureCreatedAsync(cancellationToken);

        var provisioningSucceeded = await provisioningDbContext.TenantInfrastructureProvisionings
            .AsNoTracking()
            .AnyAsync(
                x => x.TenantId == command.TenantId && x.Status == ProvisioningStatus.Succeeded,
                cancellationToken);

        if (!provisioningSucceeded)
            return null;

        return await HandleProvisioningSucceededAsync(command, cancellationToken);
    }

    public async Task<TenantKnowledgeConfigurationSetupStatusDto?> GetStatusAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        await knowledgeMetadataStoreProvisioner.EnsureCreatedAsync(cancellationToken);

        var setup = await knowledgeDbContext.TenantKnowledgeConfigurationSetups
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        return setup?.ToDto();
    }

    private async Task<TenantKnowledgeConfigurationSetup> GetOrCreateAsync(int tenantId, CancellationToken cancellationToken)
    {
        var setup = await knowledgeDbContext.TenantKnowledgeConfigurationSetups
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (setup is not null)
            return setup;

        setup = TenantKnowledgeConfigurationSetup.CreatePending(tenantId, DateTime.UtcNow);
        knowledgeDbContext.TenantKnowledgeConfigurationSetups.Add(setup);
        await knowledgeDbContext.SaveChangesAsync(cancellationToken);

        return setup;
    }
}

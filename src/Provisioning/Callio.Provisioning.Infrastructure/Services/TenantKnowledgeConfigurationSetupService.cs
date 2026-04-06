using Callio.Provisioning.Application.KnowledgeConfigurations;
using Callio.Provisioning.Domain;
using Callio.Provisioning.Domain.Enums;
using Callio.Provisioning.Infrastructure.Persistence;
using Callio.Provisioning.Infrastructure.Provisioners;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Callio.Provisioning.Infrastructure.Services;

public class TenantKnowledgeConfigurationSetupService(
    ProvisioningDbContext provisioningDbContext,
    IProvisioningMetadataStoreProvisioner provisioningMetadataStoreProvisioner,
    ITenantKnowledgeConfigurationService tenantKnowledgeConfigurationService,
    ILogger<TenantKnowledgeConfigurationSetupService> logger) : ITenantKnowledgeConfigurationSetupService
{
    public async Task EnsurePendingAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        await provisioningMetadataStoreProvisioner.EnsureCreatedAsync(cancellationToken);

        var setup = await provisioningDbContext.TenantKnowledgeConfigurationSetups
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (setup is null)
        {
            provisioningDbContext.TenantKnowledgeConfigurationSetups.Add(
                TenantKnowledgeConfigurationSetup.CreatePending(tenantId, DateTime.UtcNow));
            await provisioningDbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        if (setup.Status == KnowledgeConfigurationSetupStatus.Succeeded)
            return;

        setup.RefreshPending(DateTime.UtcNow);
        await provisioningDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TenantKnowledgeConfigurationSetupStatusDto> HandleProvisioningSucceededAsync(
        RunTenantKnowledgeConfigurationSetupCommand command,
        CancellationToken cancellationToken = default)
    {
        await provisioningMetadataStoreProvisioner.EnsureCreatedAsync(cancellationToken);

        var setup = await GetOrCreateAsync(command.TenantId, cancellationToken);
        var activeConfiguration = await tenantKnowledgeConfigurationService.GetActiveAsync(command.TenantId, cancellationToken);

        if (setup.Status == KnowledgeConfigurationSetupStatus.Succeeded && activeConfiguration is not null)
        {
            if (setup.ActiveConfigurationId != activeConfiguration.Id)
            {
                setup.MarkSucceeded(activeConfiguration.Id, DateTime.UtcNow);
                await provisioningDbContext.SaveChangesAsync(cancellationToken);
            }

            return setup.ToDto();
        }

        setup.BeginAttempt(DateTime.UtcNow);
        await provisioningDbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var configuration = await tenantKnowledgeConfigurationService.CreateDefaultAsync(
                new CreateDefaultTenantKnowledgeConfigurationCommand(command.TenantId),
                cancellationToken);

            setup.MarkSucceeded(configuration.Id, DateTime.UtcNow);
            await provisioningDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            setup.MarkFailed(ex.GetBaseException().Message, DateTime.UtcNow);
            await provisioningDbContext.SaveChangesAsync(cancellationToken);

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
        await provisioningMetadataStoreProvisioner.EnsureCreatedAsync(cancellationToken);

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
        await provisioningMetadataStoreProvisioner.EnsureCreatedAsync(cancellationToken);

        var setup = await provisioningDbContext.TenantKnowledgeConfigurationSetups
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        return setup?.ToDto();
    }

    private async Task<TenantKnowledgeConfigurationSetup> GetOrCreateAsync(int tenantId, CancellationToken cancellationToken)
    {
        var setup = await provisioningDbContext.TenantKnowledgeConfigurationSetups
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (setup is not null)
            return setup;

        setup = TenantKnowledgeConfigurationSetup.CreatePending(tenantId, DateTime.UtcNow);
        provisioningDbContext.TenantKnowledgeConfigurationSetups.Add(setup);
        await provisioningDbContext.SaveChangesAsync(cancellationToken);

        return setup;
    }
}

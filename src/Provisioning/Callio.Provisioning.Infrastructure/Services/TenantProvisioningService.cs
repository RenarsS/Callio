using Callio.Admin.Infrastructure.Persistence;
using Callio.Provisioning.Application.KnowledgeConfigurations;
using Callio.Core.Infrastructure.Messaging.Tenants;
using Callio.Provisioning.Application;
using Callio.Provisioning.Domain;
using Callio.Provisioning.Domain.Enums;
using Callio.Provisioning.Infrastructure.Persistence;
using Callio.Provisioning.Infrastructure.Provisioners;
using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Callio.Provisioning.Infrastructure.Services;

public class TenantProvisioningService(
    AdminDbContext adminDbContext,
    ProvisioningDbContext provisioningDbContext,
    ITenantDatabaseSchemaProvisioner tenantDatabaseSchemaProvisioner,
    IProvisioningMetadataStoreProvisioner provisioningMetadataStoreProvisioner,
    ITenantVectorStoreProvisioner tenantVectorStoreProvisioner,
    ITenantKnowledgeConfigurationSetupService tenantKnowledgeConfigurationSetupService,
    ITenantKnowledgeConfigurationRepository tenantKnowledgeConfigurationRepository,
    ITenantResourceNamingStrategy tenantResourceNamingStrategy,
    IPublishEndpoint publishEndpoint,
    IOptions<Callio.Provisioning.Infrastructure.Options.TenantProvisioningOptions> options,
    ILogger<TenantProvisioningService> logger) : ITenantProvisioningService
{
    private readonly KnowledgeModelConstraintsDto _models = new(
        options.Value.EmbeddingProvider,
        options.Value.EmbeddingModel,
        options.Value.GenerationModel);

    public async Task<TenantProvisioningStatusDto> HandleTenantApprovedAsync(
        TenantApprovedProvisioningCommand command,
        CancellationToken cancellationToken = default)
    {
        var provisioning = await provisioningDbContext.TenantInfrastructureProvisionings
            .Include(x => x.Steps)
            .FirstOrDefaultAsync(x => x.TenantId == command.TenantId, cancellationToken);

        if (provisioning is null)
        {
            var names = tenantResourceNamingStrategy.Create(command.TenantId);
            provisioning = TenantInfrastructureProvisioning.Create(
                command.UserId,
                command.TenantId,
                command.TenantRequestId,
                names.DatabaseSchema,
                names.VectorStoreNamespace,
                DateTime.UtcNow);

            provisioningDbContext.TenantInfrastructureProvisionings.Add(provisioning);
        }
        else
        {
            provisioning.RefreshSource(command.UserId, command.TenantRequestId, DateTime.UtcNow);
        }

        await tenantKnowledgeConfigurationSetupService.EnsurePendingAsync(command.TenantId, cancellationToken);

        if (provisioning.Status is ProvisioningStatus.InProgress or ProvisioningStatus.Succeeded or ProvisioningStatus.Failed)
        {
            await provisioningDbContext.SaveChangesAsync(cancellationToken);
            return (await GetStatusAsync(provisioning.TenantId, cancellationToken))!;
        }

        return await ExecuteAsync(provisioning, TenantProvisioningExecutionMode.Initial, cancellationToken);
    }

    public async Task<IReadOnlyList<TenantProvisioningStatusDto>> GetAllStatusesAsync(CancellationToken cancellationToken = default)
    {
        var provisionings = await provisioningDbContext.TenantInfrastructureProvisionings
            .AsNoTracking()
            .Include(x => x.Steps)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

        if (provisionings.Count == 0)
            return [];

        var tenantIds = provisionings
            .Select(p => p.TenantId)
            .ToList();

        var setupLookup = await GetKnowledgeConfigurationSetupLookupAsync(tenantIds, cancellationToken);

        var settingsLookup = await tenantKnowledgeConfigurationRepository.GetActiveByTenantIdsAsync(tenantIds, cancellationToken);

        return provisionings
            .Select(provisioning => Map(
                provisioning,
                setupLookup.GetValueOrDefault(provisioning.TenantId),
                settingsLookup.GetValueOrDefault(provisioning.TenantId)))
            .ToList();
    }

    public async Task<TenantProvisioningStatusDto?> GetStatusAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var provisioning = await provisioningDbContext.TenantInfrastructureProvisionings
            .AsNoTracking()
            .Include(x => x.Steps)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (provisioning is null)
            return null;

        var setup = await GetKnowledgeConfigurationSetupAsync(provisioning.TenantId, cancellationToken);

        var settings = await tenantKnowledgeConfigurationRepository.GetActiveAsync(provisioning.TenantId, cancellationToken);

        return Map(provisioning, setup, settings);
    }

    public async Task<TenantProvisioningStatusDto?> RetryFailedAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var provisioning = await provisioningDbContext.TenantInfrastructureProvisionings
            .Include(x => x.Steps)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (provisioning is null)
            return null;

        if (provisioning.Status == ProvisioningStatus.InProgress)
            return (await GetStatusAsync(provisioning.TenantId, cancellationToken))!;

        return await ExecuteAsync(provisioning, TenantProvisioningExecutionMode.RetryFailed, cancellationToken);
    }

    public async Task<TenantProvisioningStatusDto?> ReprovisionAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        var provisioning = await provisioningDbContext.TenantInfrastructureProvisionings
            .Include(x => x.Steps)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (provisioning is null)
        {
            provisioning = await CreateProvisioningForTenantAsync(tenantId, cancellationToken);
            if (provisioning is null)
                return null;
        }

        if (provisioning.Status == ProvisioningStatus.InProgress)
            return await GetStatusAsync(tenantId, cancellationToken);

        return await ExecuteAsync(provisioning, TenantProvisioningExecutionMode.Reprovision, cancellationToken);
    }

    private async Task<TenantProvisioningStatusDto> ExecuteAsync(
        TenantInfrastructureProvisioning provisioning,
        TenantProvisioningExecutionMode mode,
        CancellationToken cancellationToken)
    {
        var stepNamesToExecute = GetStepNamesToExecute(provisioning, mode);
        if (stepNamesToExecute.Count == 0)
            return (await GetStatusAsync(provisioning.TenantId, cancellationToken))!;

        var now = DateTime.UtcNow;
        foreach (var stepName in stepNamesToExecute)
        {
            provisioning.GetRequiredStep(stepName).Reset(now);
        }

        provisioning.BeginAttempt(now);
        await provisioningDbContext.SaveChangesAsync(cancellationToken);

        var stepsToExecute = stepNamesToExecute.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var step in provisioning.Steps.OrderBy(x => x.Order))
        {
            if (!stepsToExecute.Contains(step.Name))
                continue;

            step.MarkInProgress(DateTime.UtcNow);
            await provisioningDbContext.SaveChangesAsync(cancellationToken);

            try
            {
                await ExecuteStepAsync(provisioning, step.Name, cancellationToken);
                step.MarkSucceeded(DateTime.UtcNow);
                await provisioningDbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                var message = ex.GetBaseException().Message;
                var failedAt = DateTime.UtcNow;

                step.MarkFailed(message, failedAt);
                provisioning.MarkFailed(step.Name, message, failedAt);

                await provisioningDbContext.SaveChangesAsync(cancellationToken);
                await PublishFailureAsync(provisioning, cancellationToken);

                return (await GetStatusAsync(provisioning.TenantId, cancellationToken))!;
            }
        }

        provisioning.MarkSucceeded(DateTime.UtcNow);
        await provisioningDbContext.SaveChangesAsync(cancellationToken);
        await PublishSuccessAsync(provisioning, cancellationToken);

        return (await GetStatusAsync(provisioning.TenantId, cancellationToken))!;
    }

    private async Task<TenantInfrastructureProvisioning?> CreateProvisioningForTenantAsync(int tenantId, CancellationToken cancellationToken)
    {
        var source = await ResolveProvisioningSourceAsync(tenantId, cancellationToken);
        if (source is null)
            return null;

        var names = tenantResourceNamingStrategy.Create(tenantId);
        var provisioning = TenantInfrastructureProvisioning.Create(
            source.RequestedByUserId,
            tenantId,
            source.TenantRequestId,
            names.DatabaseSchema,
            names.VectorStoreNamespace,
            DateTime.UtcNow);

        provisioningDbContext.TenantInfrastructureProvisionings.Add(provisioning);
        await provisioningDbContext.SaveChangesAsync(cancellationToken);
        await tenantKnowledgeConfigurationSetupService.EnsurePendingAsync(tenantId, cancellationToken);

        return provisioning;
    }

    private async Task<ProvisioningSource?> ResolveProvisioningSourceAsync(int tenantId, CancellationToken cancellationToken)
    {
        var request = await adminDbContext.TenantCreationRequests
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.ProcessedAtUtc ?? x.RequestedAtUtc)
            .Select(x => new ProvisioningSource(x.RequestedByUserId, x.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (request is not null)
            return request;

        var tenantExists = await adminDbContext.Tenants
            .AsNoTracking()
            .AnyAsync(x => x.Id == tenantId, cancellationToken);

        return tenantExists ? new ProvisioningSource("dashboard-admin", 0) : null;
    }

    private async Task ExecuteStepAsync(
        TenantInfrastructureProvisioning provisioning,
        string stepName,
        CancellationToken cancellationToken)
    {
        switch (stepName)
        {
            case TenantProvisioningSteps.DatabaseSchema:
                await tenantDatabaseSchemaProvisioner.EnsureCreatedAsync(provisioning.DatabaseSchema, cancellationToken);
                break;
            case TenantProvisioningSteps.VectorStore:
                await tenantVectorStoreProvisioner.EnsureCreatedAsync(provisioning.TenantId, provisioning.VectorStoreNamespace, cancellationToken);
                break;
            default:
                throw new InvalidOperationException($"Provisioning step '{stepName}' is not supported.");
        }
    }

    private async Task<IReadOnlyDictionary<int, TenantKnowledgeConfigurationSetup>> GetKnowledgeConfigurationSetupLookupAsync(
        IReadOnlyCollection<int> tenantIds,
        CancellationToken cancellationToken)
    {
        if (tenantIds.Count == 0)
            return new Dictionary<int, TenantKnowledgeConfigurationSetup>();

        try
        {
            await provisioningMetadataStoreProvisioner.EnsureCreatedAsync(cancellationToken);

            return await provisioningDbContext.TenantKnowledgeConfigurationSetups
                .AsNoTracking()
                .Where(x => tenantIds.Contains(x.TenantId))
                .ToDictionaryAsync(x => x.TenantId, cancellationToken);
        }
        catch (SqlException ex) when (IsMissingKnowledgeConfigurationSetupTable(ex))
        {
            logger.LogWarning(
                ex,
                "Tenant knowledge configuration setup status table is missing. Returning provisioning status without configuration setup details.");

            return new Dictionary<int, TenantKnowledgeConfigurationSetup>();
        }
    }

    private async Task<TenantKnowledgeConfigurationSetup?> GetKnowledgeConfigurationSetupAsync(int tenantId, CancellationToken cancellationToken)
    {
        try
        {
            await provisioningMetadataStoreProvisioner.EnsureCreatedAsync(cancellationToken);

            return await provisioningDbContext.TenantKnowledgeConfigurationSetups
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);
        }
        catch (SqlException ex) when (IsMissingKnowledgeConfigurationSetupTable(ex))
        {
            logger.LogWarning(
                ex,
                "Tenant knowledge configuration setup status table is missing for tenant {TenantId}. Returning infrastructure status without configuration setup details.",
                tenantId);

            return null;
        }
    }

    private static bool IsMissingKnowledgeConfigurationSetupTable(SqlException exception)
        => exception.Number == 208
           && exception.Message.Contains("TenantKnowledgeConfigurationSetups", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<string> GetStepNamesToExecute(
        TenantInfrastructureProvisioning provisioning,
        TenantProvisioningExecutionMode mode)
        => mode switch
        {
            TenantProvisioningExecutionMode.Initial => provisioning.Steps
                .Where(x => x.Status == ProvisioningStepStatus.Pending)
                .OrderBy(x => x.Order)
                .Select(x => x.Name)
                .ToList(),
            TenantProvisioningExecutionMode.RetryFailed => provisioning.Steps
                .Where(x => x.Status is ProvisioningStepStatus.Failed or ProvisioningStepStatus.Pending)
                .OrderBy(x => x.Order)
                .Select(x => x.Name)
                .ToList(),
            TenantProvisioningExecutionMode.Reprovision => provisioning.Steps
                .OrderBy(x => x.Order)
                .Select(x => x.Name)
                .ToList(),
            _ => []
        };

    private async Task PublishSuccessAsync(TenantInfrastructureProvisioning provisioning, CancellationToken cancellationToken)
    {
        try
        {
            await publishEndpoint.Publish(
                new TenantInfrastructureProvisioningSucceededIntegrationEvent(
                    provisioning.TenantId,
                    provisioning.TenantRequestId,
                    provisioning.RequestedByUserId,
                    provisioning.DatabaseSchema,
                    provisioning.VectorStoreNamespace,
                    provisioning.AttemptCount,
                    MapStepStatuses(provisioning),
                    DateTime.UtcNow),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Tenant infrastructure provisioning succeeded for tenant {TenantId}, but the success integration event could not be published.",
                provisioning.TenantId);
        }
    }

    private async Task PublishFailureAsync(TenantInfrastructureProvisioning provisioning, CancellationToken cancellationToken)
    {
        try
        {
            await publishEndpoint.Publish(
                new TenantInfrastructureProvisioningFailedIntegrationEvent(
                    provisioning.TenantId,
                    provisioning.TenantRequestId,
                    provisioning.RequestedByUserId,
                    provisioning.DatabaseSchema,
                    provisioning.VectorStoreNamespace,
                    provisioning.AttemptCount,
                    provisioning.FailedStep,
                    provisioning.LastError,
                    MapStepStatuses(provisioning),
                    DateTime.UtcNow),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Tenant infrastructure provisioning failed for tenant {TenantId}, but the failure integration event could not be published.",
                provisioning.TenantId);
        }
    }

    private static IReadOnlyList<TenantProvisioningStepIntegrationStatus> MapStepStatuses(TenantInfrastructureProvisioning provisioning)
        => provisioning.Steps
            .OrderBy(x => x.Order)
            .Select(x => new TenantProvisioningStepIntegrationStatus(
                x.Name,
                x.Status.ToString(),
                x.AttemptCount,
                x.LastError))
            .ToList();

    private TenantProvisioningStatusDto Map(
        TenantInfrastructureProvisioning provisioning,
        TenantKnowledgeConfigurationSetup? setup,
        TenantKnowledgeConfiguration? settings)
        => new(
            provisioning.TenantId,
            provisioning.TenantRequestId,
            provisioning.RequestedByUserId,
            provisioning.Status.ToString(),
            provisioning.AttemptCount,
            provisioning.DatabaseSchema,
            provisioning.VectorStoreNamespace,
            provisioning.FailedStep,
            provisioning.LastError,
            provisioning.CreatedAtUtc,
            provisioning.UpdatedAtUtc,
            provisioning.LastStartedAtUtc,
            provisioning.LastCompletedAtUtc,
            setup?.ToDto(),
            settings?.ToSummaryDto(_models),
            provisioning.Steps
                .OrderBy(x => x.Order)
                .Select(x => new TenantProvisioningStepDto(
                    x.Name,
                    x.Order,
                    x.Status.ToString(),
                    x.AttemptCount,
                    x.LastError,
                    x.LastStartedAtUtc,
                    x.LastCompletedAtUtc))
                .ToList());
    
    private sealed record ProvisioningSource(string RequestedByUserId, int TenantRequestId);

    private enum TenantProvisioningExecutionMode
    {
        Initial = 1,
        RetryFailed = 2,
        Reprovision = 3
    }
}

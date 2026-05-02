using Callio.Admin.Domain;
using Callio.Admin.Domain.Enums;
using Callio.Admin.Domain.ValueObjects;
using Callio.Admin.Infrastructure.Persistence;
using Callio.Identity.Domain;
using Callio.Identity.Infrastructure.Persistence;
using Callio.Generation.Application.Generation;
using Callio.Knowledge.Application.KnowledgeConfigurations;
using Callio.Knowledge.Application.KnowledgeDocuments;
using Callio.Knowledge.Domain;
using Callio.Knowledge.Domain.Enums;
using Callio.Knowledge.Infrastructure.Persistence;
using Callio.Knowledge.Infrastructure.Provisioners;
using Callio.Provisioning.Domain;
using Callio.Provisioning.Domain.Enums;
using Callio.Provisioning.Infrastructure.Persistence;
using Callio.Provisioning.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Callio.DatabaseTool;

internal sealed class TestTenantRequestSeeder(
    AdminDbContext adminDbContext,
    AppIdentityDbContext identityDbContext,
    KnowledgeDbContext knowledgeDbContext,
    ProvisioningDbContext provisioningDbContext,
    ITenantResourceNamingStrategy tenantResourceNamingStrategy,
    ITenantKnowledgeConfigurationService tenantKnowledgeConfigurationService,
    ITenantKnowledgeConfigurationSetupService tenantKnowledgeConfigurationSetupService,
    ITenantKnowledgeDocumentService tenantKnowledgeDocumentService,
    ITenantGenerationService tenantGenerationService,
    IKnowledgeMetadataStoreProvisioner knowledgeMetadataStoreProvisioner,
    TenantSchemaMigrationRunner tenantSchemaMigrationRunner,
    ILogger<TestTenantRequestSeeder> logger)
{
    private const string SeedMarker = "[seed:test-data]";
    private const string SeedProcessorUserId = "seed-runner";
    private const string SeedPassword = "SeededPassword!234";

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await knowledgeMetadataStoreProvisioner.EnsureCreatedAsync(cancellationToken);

        var plans = await adminDbContext.Plans
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Name, cancellationToken);
        var metrics = await adminDbContext.UsageMetrics
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Key, cancellationToken);
        var quotaCount = await adminDbContext.PlanQuotas.CountAsync(cancellationToken);

        var starterPlan = GetRequiredPlan(plans, "Starter");
        var growthPlan = GetRequiredPlan(plans, "Growth");
        var enterprisePlan = GetRequiredPlan(plans, "Enterprise");
        GetRequiredMetric(metrics, "documents");
        GetRequiredMetric(metrics, "storage_gb");
        GetRequiredMetric(metrics, "rag_queries");
        GetRequiredMetric(metrics, "ingestion_jobs");

        logger.LogInformation(
            "Billing catalog ready with {PlanCount} plans, {QuotaCount} quotas, and {MetricCount} usage metrics.",
            plans.Count,
            quotaCount,
            metrics.Count);

        await SeedPendingRequestAsync(starterPlan, cancellationToken);
        await SeedRejectedRequestAsync(growthPlan, cancellationToken);

        var healthyTenant = await SeedHealthyApprovedTenantAsync(growthPlan, cancellationToken);
        var provisioningFailedTenant = await SeedProvisioningFailureTenantAsync(starterPlan, cancellationToken);
        var configurationFailedTenant = await SeedConfigurationFailureTenantAsync(enterprisePlan, cancellationToken);

        var migratedSchemas = await tenantSchemaMigrationRunner.MigrateAllAsync(cancellationToken);
        logger.LogInformation("Tenant schema setup refreshed for {SchemaCount} schema(s) after seeding.", migratedSchemas);

        await EnsureHealthyKnowledgeConfigurationAsync(healthyTenant.TenantId, cancellationToken);
        await EnsureHealthyKnowledgeDocumentsAsync(healthyTenant.TenantId, cancellationToken);
        await EnsureHealthyGenerationDataAsync(healthyTenant.TenantId, cancellationToken);
        await tenantKnowledgeConfigurationSetupService.EnsurePendingAsync(provisioningFailedTenant.TenantId, cancellationToken);
        await EnsureFailedKnowledgeConfigurationSetupAsync(configurationFailedTenant.TenantId, cancellationToken);

        await EnsureUsageRecordsAsync(
            healthyTenant.TenantId,
            metrics,
            new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["documents"] = 18320m,
                ["storage_gb"] = 14.6m,
                ["rag_queries"] = 48210m,
                ["ingestion_jobs"] = 214m
            },
            cancellationToken);

        await EnsureUsageRecordsAsync(
            provisioningFailedTenant.TenantId,
            metrics,
            new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["documents"] = 2450m,
                ["storage_gb"] = 2.8m,
                ["rag_queries"] = 7910m,
                ["ingestion_jobs"] = 63m
            },
            cancellationToken);

        await EnsureUsageRecordsAsync(
            configurationFailedTenant.TenantId,
            metrics,
            new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["documents"] = 120540m,
                ["storage_gb"] = 67.4m,
                ["rag_queries"] = 189330m,
                ["ingestion_jobs"] = 1287m
            },
            cancellationToken);
    }

    private async Task SeedPendingRequestAsync(Plan plan, CancellationToken cancellationToken)
    {
        const string tenantName = "Northwind Knowledge Hub";
        const string email = "northwind.requester@seed.callio.local";
        const string firstName = "Nina";
        const string lastName = "West";
        const string companyName = "Northwind Research";
        const string preferredUserId = "seed-pending-requester";

        var request = await adminDbContext.TenantCreationRequests
            .FirstOrDefaultAsync(x => x.TenantName == tenantName && x.RequestedByEmail == email, cancellationToken);

        if (request is not null)
            return;

        var user = await EnsurePortalUserAsync(
            preferredUserId,
            email,
            firstName,
            lastName,
            "+37120000011",
            tenantId: null,
            cancellationToken);

        request = new TenantCreationRequest(
            tenantName,
            user.Id,
            email,
            firstName,
            lastName,
            companyName,
            BuildRequestNotes(plan, "Pending tenant request seeded for dashboard approval testing."),
            DateTime.UtcNow.AddDays(-5));

        adminDbContext.TenantCreationRequests.Add(request);
        await adminDbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded pending tenant request '{TenantName}'.", tenantName);
    }

    private async Task SeedRejectedRequestAsync(Plan plan, CancellationToken cancellationToken)
    {
        const string tenantName = "Contoso Archive";
        const string email = "contoso.rejected@seed.callio.local";
        const string firstName = "Chris";
        const string lastName = "Stone";
        const string companyName = "Contoso Archive";
        const string preferredUserId = "seed-rejected-requester";

        var request = await adminDbContext.TenantCreationRequests
            .FirstOrDefaultAsync(x => x.TenantName == tenantName && x.RequestedByEmail == email, cancellationToken);

        if (request is null)
        {
            var user = await EnsurePortalUserAsync(
                preferredUserId,
                email,
                firstName,
                lastName,
                "+37120000012",
                tenantId: null,
                cancellationToken);

            request = new TenantCreationRequest(
                tenantName,
                user.Id,
                email,
                firstName,
                lastName,
                companyName,
                BuildRequestNotes(plan, "Rejected tenant request seeded for dashboard history testing."),
                DateTime.UtcNow.AddDays(-4));

            request.Reject(
                SeedProcessorUserId,
                "Seeded rejection: incomplete onboarding information.",
                DateTime.UtcNow.AddDays(-3));

            adminDbContext.TenantCreationRequests.Add(request);
            await adminDbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Ensured rejected tenant request '{TenantName}'.", tenantName);
    }

    private async Task<SeededTenantContext> SeedHealthyApprovedTenantAsync(Plan plan, CancellationToken cancellationToken)
    {
        const string tenantName = "Acme Intelligence";
        const string email = "acme.admin@seed.callio.local";
        const string firstName = "Avery";
        const string lastName = "Cole";
        const string companyName = "Acme Intelligence";
        const string preferredUserId = "seed-acme-admin";

        var requestedAt = DateTime.UtcNow.AddDays(-3);
        var tenant = await EnsureTenantAsync(
            tenantName,
            email,
            firstName,
            lastName,
            "+37120000021",
            createdAt: requestedAt,
            cancellationToken);

        var user = await EnsurePortalUserAsync(
            preferredUserId,
            email,
            firstName,
            lastName,
            "+37120000021",
            tenant.Id,
            cancellationToken);

        var request = await EnsureApprovedRequestAsync(
            tenant,
            user.Id,
            email,
            firstName,
            lastName,
            companyName,
            BuildRequestNotes(plan, "Healthy approved tenant seeded for infrastructure and configuration testing."),
            requestedAt,
            cancellationToken);

        await EnsureSubscriptionAsync(tenant.Id, plan.Id, SubscriptionStatus.Active, cancellationToken);
        await EnsureSucceededProvisioningAsync(tenant.Id, request.Id, requestedAt, cancellationToken);

        logger.LogInformation("Ensured healthy approved tenant '{TenantName}'.", tenantName);

        return new SeededTenantContext(tenant.Id, request.Id);
    }

    private async Task<SeededTenantContext> SeedProvisioningFailureTenantAsync(Plan plan, CancellationToken cancellationToken)
    {
        const string tenantName = "Bluebird Support";
        const string email = "bluebird.admin@seed.callio.local";
        const string firstName = "Bianca";
        const string lastName = "Rivera";
        const string companyName = "Bluebird Support";
        const string preferredUserId = "seed-bluebird-admin";

        var requestedAt = DateTime.UtcNow.AddDays(-2);
        var tenant = await EnsureTenantAsync(
            tenantName,
            email,
            firstName,
            lastName,
            "+37120000022",
            createdAt: requestedAt,
            cancellationToken);

        var user = await EnsurePortalUserAsync(
            preferredUserId,
            email,
            firstName,
            lastName,
            "+37120000022",
            tenant.Id,
            cancellationToken);

        var request = await EnsureApprovedRequestAsync(
            tenant,
            user.Id,
            email,
            firstName,
            lastName,
            companyName,
            BuildRequestNotes(plan, "Approved tenant seeded with a failed infrastructure run."),
            requestedAt,
            cancellationToken);

        await EnsureSubscriptionAsync(tenant.Id, plan.Id, SubscriptionStatus.Trial, cancellationToken);
        await EnsureFailedProvisioningAsync(tenant.Id, request.Id, requestedAt, cancellationToken);

        logger.LogInformation("Ensured provisioning-failure tenant '{TenantName}'.", tenantName);

        return new SeededTenantContext(tenant.Id, request.Id);
    }

    private async Task<SeededTenantContext> SeedConfigurationFailureTenantAsync(Plan plan, CancellationToken cancellationToken)
    {
        const string tenantName = "Fabrikam Knowledge";
        const string email = "fabrikam.admin@seed.callio.local";
        const string firstName = "Farah";
        const string lastName = "Kim";
        const string companyName = "Fabrikam Knowledge";
        const string preferredUserId = "seed-fabrikam-admin";

        var requestedAt = DateTime.UtcNow.AddDays(-1);
        var tenant = await EnsureTenantAsync(
            tenantName,
            email,
            firstName,
            lastName,
            "+37120000023",
            createdAt: requestedAt,
            cancellationToken);

        var user = await EnsurePortalUserAsync(
            preferredUserId,
            email,
            firstName,
            lastName,
            "+37120000023",
            tenant.Id,
            cancellationToken);

        var request = await EnsureApprovedRequestAsync(
            tenant,
            user.Id,
            email,
            firstName,
            lastName,
            companyName,
            BuildRequestNotes(plan, "Approved tenant seeded with a failed knowledge configuration setup."),
            requestedAt,
            cancellationToken);

        await EnsureSubscriptionAsync(tenant.Id, plan.Id, SubscriptionStatus.PastDue, cancellationToken);
        await EnsureSucceededProvisioningAsync(tenant.Id, request.Id, requestedAt, cancellationToken);

        logger.LogInformation("Ensured configuration-failure tenant '{TenantName}'.", tenantName);

        return new SeededTenantContext(tenant.Id, request.Id);
    }

    private async Task<ApplicationUser> EnsurePortalUserAsync(
        string preferredUserId,
        string email,
        string firstName,
        string lastName,
        string phoneNumber,
        int? tenantId,
        CancellationToken cancellationToken)
    {
        var existingUser = await identityDbContext.Users
            .FirstOrDefaultAsync(
                x => x.Id == preferredUserId || x.Email == email,
                cancellationToken);

        if (existingUser is not null)
        {
            if (tenantId.HasValue && existingUser.TenantId != tenantId)
            {
                existingUser.LinkToTenant(tenantId.Value);
                await identityDbContext.SaveChangesAsync(cancellationToken);
            }

            return existingUser;
        }

        var user = ApplicationUser.CreateTenantUser(email, firstName, lastName, tenantId);
        user.Id = preferredUserId;
        user.EmailConfirmed = true;
        user.PhoneNumber = phoneNumber;
        user.PhoneNumberConfirmed = true;
        user.NormalizedEmail = email.ToUpperInvariant();
        user.NormalizedUserName = email.ToUpperInvariant();
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.ConcurrencyStamp = Guid.NewGuid().ToString("N");
        user.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, SeedPassword);

        if (tenantId.HasValue)
            user.LinkToTenant(tenantId.Value);

        identityDbContext.Users.Add(user);
        await identityDbContext.SaveChangesAsync(cancellationToken);

        return user;
    }

    private async Task<Tenant> EnsureTenantAsync(
        string tenantName,
        string email,
        string firstName,
        string lastName,
        string phoneNumber,
        DateTime createdAt,
        CancellationToken cancellationToken)
    {
        var existingTenant = await adminDbContext.Tenants
            .FirstOrDefaultAsync(x => x.Name == tenantName, cancellationToken);

        if (existingTenant is not null)
            return existingTenant;

        var tenant = new Tenant(
            tenantName,
            null,
            new Contact(
                $"{firstName} {lastName}",
                email,
                phoneNumber,
                new Address("Seed Street 1", "LV-1010", "Riga", "Latvia"),
                "https://callio.local"),
            createdAt,
            createdAt)
        {
            Name = tenantName
        };

        adminDbContext.Tenants.Add(tenant);
        await adminDbContext.SaveChangesAsync(cancellationToken);

        return tenant;
    }

    private async Task<TenantCreationRequest> EnsureApprovedRequestAsync(
        Tenant tenant,
        string requestedByUserId,
        string email,
        string firstName,
        string lastName,
        string companyName,
        string notes,
        DateTime requestedAtUtc,
        CancellationToken cancellationToken)
    {
        var request = await adminDbContext.TenantCreationRequests
            .FirstOrDefaultAsync(
                x => x.TenantName == tenant.Name && x.RequestedByEmail == email,
                cancellationToken);

        if (request is null)
        {
            request = new TenantCreationRequest(
                tenant.Name,
                requestedByUserId,
                email,
                firstName,
                lastName,
                companyName,
                notes,
                requestedAtUtc);

            request.Approve(
                tenant.Id,
                SeedProcessorUserId,
                "Seeded approval for dashboard workflow testing.",
                requestedAtUtc.AddHours(4));

            adminDbContext.TenantCreationRequests.Add(request);
            await adminDbContext.SaveChangesAsync(cancellationToken);

            return request;
        }

        if (request.Status == TenantRequestStatus.Pending)
        {
            request.Approve(
                tenant.Id,
                SeedProcessorUserId,
                "Seeded approval for dashboard workflow testing.",
                requestedAtUtc.AddHours(4));

            await adminDbContext.SaveChangesAsync(cancellationToken);
            return request;
        }

        if (request.Status == TenantRequestStatus.Approved && request.TenantId == tenant.Id)
            return request;

        throw new InvalidOperationException(
            $"Seeded approved request '{tenant.Name}' already exists with status '{request.Status}' and cannot be reconciled automatically.");
    }

    private async Task EnsureSubscriptionAsync(
        int tenantId,
        int planId,
        SubscriptionStatus desiredStatus,
        CancellationToken cancellationToken)
    {
        var existingSubscription = await adminDbContext.Subscriptions
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (existingSubscription is not null)
            return;

        var now = DateTime.UtcNow;
        var subscription = new Subscription(
            tenantId,
            planId,
            new DateRange(now.AddDays(-7), now.AddMonths(1), now),
            desiredStatus == SubscriptionStatus.Trial ? now.AddDays(10) : null);

        switch (desiredStatus)
        {
            case SubscriptionStatus.Active:
            case SubscriptionStatus.Trial:
                break;
            case SubscriptionStatus.PastDue:
                subscription.MarkPastDue();
                break;
            case SubscriptionStatus.Cancelled:
                subscription.Cancel(true, now.AddDays(-1));
                break;
            case SubscriptionStatus.Suspended:
                subscription.Suspend();
                break;
            case SubscriptionStatus.Pending:
                subscription.Renew(new DateRange(now.AddDays(7), now.AddMonths(2), now));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(desiredStatus), desiredStatus, "Unsupported seeded subscription status.");
        }

        adminDbContext.Subscriptions.Add(subscription);
        await adminDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSucceededProvisioningAsync(
        int tenantId,
        int requestId,
        DateTime requestedAtUtc,
        CancellationToken cancellationToken)
    {
        var existingProvisioning = await provisioningDbContext.TenantInfrastructureProvisionings
            .Include(x => x.Steps)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (existingProvisioning is not null)
            return;

        var names = tenantResourceNamingStrategy.Create(tenantId);
        var startedAt = requestedAtUtc.AddHours(6);
        var completedAt = startedAt.AddMinutes(20);

        var provisioning = TenantInfrastructureProvisioning.Create(
            SeedProcessorUserId,
            tenantId,
            requestId,
            names.DatabaseSchema,
            names.VectorStoreNamespace,
            names.BlobContainerName,
            requestedAtUtc.AddHours(5));

        provisioning.BeginAttempt(startedAt);

        var databaseStep = provisioning.GetRequiredStep(TenantProvisioningSteps.DatabaseSchema);
        databaseStep.MarkInProgress(startedAt.AddMinutes(1));
        databaseStep.MarkSucceeded(startedAt.AddMinutes(5));

        var vectorStep = provisioning.GetRequiredStep(TenantProvisioningSteps.VectorStore);
        vectorStep.MarkInProgress(startedAt.AddMinutes(6));
        vectorStep.MarkSucceeded(startedAt.AddMinutes(15));

        var blobStep = provisioning.GetRequiredStep(TenantProvisioningSteps.BlobStorage);
        blobStep.MarkInProgress(startedAt.AddMinutes(16));
        blobStep.MarkSucceeded(startedAt.AddMinutes(19));

        provisioning.MarkSucceeded(completedAt);

        provisioningDbContext.TenantInfrastructureProvisionings.Add(provisioning);
        await provisioningDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureFailedProvisioningAsync(
        int tenantId,
        int requestId,
        DateTime requestedAtUtc,
        CancellationToken cancellationToken)
    {
        var existingProvisioning = await provisioningDbContext.TenantInfrastructureProvisionings
            .Include(x => x.Steps)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (existingProvisioning is not null)
            return;

        var names = tenantResourceNamingStrategy.Create(tenantId);
        var startedAt = requestedAtUtc.AddHours(5);
        var failedAt = startedAt.AddMinutes(18);
        const string error = "Seeded failure: vector store namespace provisioning timed out.";

        var provisioning = TenantInfrastructureProvisioning.Create(
            SeedProcessorUserId,
            tenantId,
            requestId,
            names.DatabaseSchema,
            names.VectorStoreNamespace,
            names.BlobContainerName,
            requestedAtUtc.AddHours(4));

        provisioning.BeginAttempt(startedAt);

        var databaseStep = provisioning.GetRequiredStep(TenantProvisioningSteps.DatabaseSchema);
        databaseStep.MarkInProgress(startedAt.AddMinutes(1));
        databaseStep.MarkSucceeded(startedAt.AddMinutes(4));

        var vectorStep = provisioning.GetRequiredStep(TenantProvisioningSteps.VectorStore);
        vectorStep.MarkInProgress(startedAt.AddMinutes(5));
        vectorStep.MarkFailed(error, failedAt);

        provisioning.MarkFailed(TenantProvisioningSteps.VectorStore, error, failedAt);

        provisioningDbContext.TenantInfrastructureProvisionings.Add(provisioning);
        await provisioningDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureHealthyKnowledgeConfigurationAsync(int tenantId, CancellationToken cancellationToken)
    {
        await tenantKnowledgeConfigurationSetupService.HandleProvisioningSucceededAsync(
            new RunTenantKnowledgeConfigurationSetupCommand(tenantId),
            cancellationToken);

        var activeConfiguration = await tenantKnowledgeConfigurationService.GetActiveAsync(tenantId, cancellationToken);
        if (activeConfiguration is null)
            throw new InvalidOperationException($"Expected an active knowledge configuration for seeded tenant {tenantId}.");

        await tenantKnowledgeConfigurationService.UpdateAsync(
            new UpdateTenantKnowledgeConfigurationCommand(
                tenantId,
                activeConfiguration.Id,
                "You are the Acme Intelligence assistant. Use approved Acme knowledge only, answer concisely, and call out uncertainty explicitly.",
                "Retrieve only the best supporting passages, keep answers grounded in the retrieved material, and avoid speculative synthesis.",
                1200,
                180,
                10,
                8,
                0.78m,
                [".pdf", ".docx", ".md", ".txt", ".csv"],
                15 * 1024 * 1024,
                true,
                false,
                true),
            cancellationToken);
    }

    private async Task EnsureFailedKnowledgeConfigurationSetupAsync(int tenantId, CancellationToken cancellationToken)
    {
        var activeConfiguration = await tenantKnowledgeConfigurationService.GetActiveAsync(tenantId, cancellationToken);
        if (activeConfiguration is not null)
        {
            logger.LogInformation(
                "Skipping seeded configuration failure for tenant {TenantId} because an active configuration already exists.",
                tenantId);

            return;
        }

        var setup = await knowledgeDbContext.TenantKnowledgeConfigurationSetups
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (setup is null)
        {
            setup = TenantKnowledgeConfigurationSetup.CreatePending(tenantId, DateTime.UtcNow.AddHours(-4));
            knowledgeDbContext.TenantKnowledgeConfigurationSetups.Add(setup);
        }

        if (setup.Status != KnowledgeConfigurationSetupStatus.Failed)
        {
            var startedAt = DateTime.UtcNow.AddHours(-3);
            setup.BeginAttempt(startedAt);
            setup.MarkFailed(
                "Seeded failure: knowledge configuration defaults could not be applied because the approval policy service was unavailable.",
                startedAt.AddMinutes(12));

            await knowledgeDbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureHealthyGenerationDataAsync(int tenantId, CancellationToken cancellationToken)
    {
        var prompts = await tenantGenerationService.GetPromptTemplatesAsync(tenantId, cancellationToken);

        var executiveBrief = prompts.FirstOrDefault(x => x.Key == "executive-brief");
        if (executiveBrief is null)
        {
            executiveBrief = await tenantGenerationService.CreatePromptTemplateAsync(
                new CreateTenantGenerationPromptTemplateCommand(
                    tenantId,
                    "executive-brief",
                    "Executive brief",
                    "Summarize retrieved tenant knowledge into a short executive-ready brief.",
                    "You write concise executive summaries for the tenant. Use only the retrieved tenant context and clearly state when evidence is missing.",
                    "Topic:\n{{input}}\n\nTenant instructions:\n{{assistantInstructionPrompt}}\n\nContext:\n{{context}}\n\nWrite a short executive brief with three bullets and cite sources.",
                    [new GenerationDataSourceSelectionDto("KnowledgeChunk", null, null, null, null, null, null, false)]),
                cancellationToken);
        }

        var existingResponses = await tenantGenerationService.GetResponsesAsync(
            tenantId,
            new GetTenantGenerationResponsesQuery(10),
            cancellationToken);

        if (existingResponses.Any(x => x.PromptKey == "knowledge-answer"))
        {
            logger.LogInformation(
                "Skipping seeded generation responses for tenant {TenantId} because response storage already contains sample data.",
                tenantId);

            return;
        }

        await tenantGenerationService.GenerateAsync(
            new GenerateTenantResponseCommand(
                tenantId,
                "What is the current onboarding status for Acme Intelligence?",
                "knowledge-answer",
                [],
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                true,
                "seed-acme-admin",
                "Avery Cole"),
            cancellationToken);

        await tenantGenerationService.GenerateAsync(
            new GenerateTenantResponseCommand(
                tenantId,
                "Provide a short executive brief for the Acme Intelligence rollout.",
                executiveBrief.Key,
                [],
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                true,
                "seed-acme-admin",
                "Avery Cole"),
            cancellationToken);
    }

    private async Task EnsureHealthyKnowledgeDocumentsAsync(int tenantId, CancellationToken cancellationToken)
    {
        var existingDocuments = await tenantKnowledgeDocumentService.GetDocumentsAsync(
            tenantId,
            new GetTenantKnowledgeDocumentsQuery(null, null, null),
            cancellationToken);

        if (existingDocuments.Count > 0)
        {
            logger.LogInformation(
                "Skipping seeded knowledge library documents for tenant {TenantId} because the tenant schema already contains uploaded documents.",
                tenantId);

            return;
        }

        await tenantKnowledgeDocumentService.UploadAsync(
            new UploadTenantKnowledgeDocumentCommand(
                tenantId,
                "Acme rollout overview",
                "acme-rollout-overview.md",
                "text/markdown",
                Encoding.UTF8.GetBytes(
                    """
                    # Acme rollout overview

                    Acme Intelligence has completed tenant approval and infrastructure provisioning.

                    The customer success team should direct portal users to the customer dashboard for provisioning, knowledge settings, prompt storage, response storage, and the knowledge library.

                    Knowledge ingestion for Acme should prioritize approved operating procedures, implementation notes, and customer-facing support material.
                    """),
                CategoryId: null,
                CategoryName: "Operations",
                TagIds: [],
                TagNames: ["rollout", "onboarding", "dashboard"],
                UploadedByUserId: "seed-acme-admin",
                UploadedByDisplayName: "Avery Cole",
                ApproveForIndexing: true,
                SourceType: KnowledgeDocumentSourceType.ManualUpload),
            cancellationToken);

        await tenantKnowledgeDocumentService.UploadAsync(
            new UploadTenantKnowledgeDocumentCommand(
                tenantId,
                "Acme support playbook",
                "acme-support-playbook.txt",
                "text/plain",
                Encoding.UTF8.GetBytes(
                    """
                    Acme support playbook

                    1. Confirm the tenant provisioning status from the portal before escalating onboarding issues.
                    2. Review the active knowledge settings before bulk document uploads.
                    3. Store reusable answer patterns in prompt storage and audit important outputs in response storage.
                    4. Escalate failed ingestion or provisioning runs with the recorded error details.
                    """),
                CategoryId: null,
                CategoryName: "Support",
                TagIds: [],
                TagNames: ["support", "knowledge", "responses"],
                UploadedByUserId: "seed-acme-admin",
                UploadedByDisplayName: "Avery Cole",
                ApproveForIndexing: true,
                SourceType: KnowledgeDocumentSourceType.ManualUpload),
            cancellationToken);
    }

    private static Plan GetRequiredPlan(IReadOnlyDictionary<string, Plan> plans, string planName)
        => plans.TryGetValue(planName, out var plan)
            ? plan
            : throw new InvalidOperationException($"Required plan '{planName}' was not found. Run the baseline admin seed first.");

    private static UsageMetric GetRequiredMetric(IReadOnlyDictionary<string, UsageMetric> metrics, string metricKey)
        => metrics.TryGetValue(metricKey, out var metric)
            ? metric
            : throw new InvalidOperationException($"Required usage metric '{metricKey}' was not found. Run the baseline admin seed first.");

    private static string BuildRequestNotes(Plan plan, string details)
        => string.Join(
            Environment.NewLine,
            $"Selected subscription plan id: {plan.Id}",
            $"Selected subscription plan: {plan.Name}",
            string.Empty,
            SeedMarker,
            details);

    private async Task EnsureUsageRecordsAsync(
        int tenantId,
        IReadOnlyDictionary<string, UsageMetric> metrics,
        IReadOnlyDictionary<string, decimal> quantitiesByMetricKey,
        CancellationToken cancellationToken)
    {
        var addedAny = false;
        var offset = 0;

        foreach (var entry in quantitiesByMetricKey)
        {
            var metric = GetRequiredMetric(metrics, entry.Key);
            var sourceReference = BuildUsageSourceReference(tenantId, entry.Key);

            var exists = await adminDbContext.UsageRecords
                .AnyAsync(
                    x => x.TenantId == tenantId
                         && x.UsageMetricId == metric.Id
                         && x.SourceReference == sourceReference,
                    cancellationToken);

            if (exists)
            {
                offset++;
                continue;
            }

            adminDbContext.UsageRecords.Add(
                new UsageRecord(
                    tenantId,
                    metric.Id,
                    entry.Value,
                    DateTime.UtcNow.AddMinutes(-(offset + 1) * 7),
                    sourceReference));

            addedAny = true;
            offset++;
        }

        if (addedAny)
        {
            await adminDbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Seeded usage values for tenant {TenantId} across {MetricCount} metrics.",
                tenantId,
                quantitiesByMetricKey.Count);
        }
    }

    private static string BuildUsageSourceReference(int tenantId, string metricKey)
        => $"{SeedMarker}:tenant:{tenantId}:metric:{metricKey}";

    private sealed record SeededTenantContext(int TenantId, int RequestId);
}

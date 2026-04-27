using System.Text.RegularExpressions;
using Callio.Admin.Application.Tenants;
using Callio.Admin.Domain;
using Callio.Admin.Domain.Enums;
using Callio.Admin.Domain.ValueObjects;
using Callio.Admin.Infrastructure.Persistence;
using Callio.Core.Infrastructure.Messaging.Tenants;
using Callio.Identity.Domain;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Callio.Admin.Infrastructure.Services;

public class TenantRequestService(
    AdminDbContext adminDbContext,
    UserManager<ApplicationUser> userManager,
    IPublishEndpoint publishEndpoint) : ITenantRequestService
{
    public async Task<PortalTenantOnboardingResultDto> RegisterPortalUserAndRequestTenantAsync(
        RegisterPortalUserAndTenantCommand command,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanSubmitRequestAsync(command.Email, command.TenantName, cancellationToken);
        var selectedPlan = await ResolveSelectedPlanAsync(command.SelectedPlanId, cancellationToken);

        var existingUser = await userManager.FindByEmailAsync(command.Email);
        if (existingUser is not null)
            throw new InvalidOperationException("A user with this email already exists.");

        var user = ApplicationUser.CreateTenantUser(command.Email, command.FirstName, command.LastName);
        var createUserResult = await userManager.CreateAsync(user, command.Password);
        if (!createUserResult.Succeeded)
        {
            var errors = string.Join("; ", createUserResult.Errors.Select(x => x.Description));
            throw new InvalidOperationException($"Portal user registration failed: {errors}");
        }

        try
        {
            var storedNotes = BuildStoredNotes(command.Notes, selectedPlan);
            var request = new TenantCreationRequest(
                command.TenantName,
                user.Id,
                command.Email,
                command.FirstName,
                command.LastName,
                command.CompanyName,
                storedNotes,
                DateTime.UtcNow);

            adminDbContext.TenantCreationRequests.Add(request);
            await adminDbContext.SaveChangesAsync(cancellationToken);

            return new PortalTenantOnboardingResultDto(
                user.Id,
                request.Id,
                command.Email,
                request.TenantName,
                request.CompanyName,
                request.Status.ToString(),
                "Portal user registered and tenant request submitted for Callio dashboard review.",
                selectedPlan?.Id,
                selectedPlan?.Name);
        }
        catch
        {
            await userManager.DeleteAsync(user);
            throw;
        }
    }

    public async Task<PortalTenantRequestStatusDto?> GetPortalStatusAsync(
        int requestId,
        string email,
        CancellationToken cancellationToken = default)
    {
        var trimmedEmail = email.Trim();

        var request = await adminDbContext.TenantCreationRequests
            .AsNoTracking()
            .Where(x => x.Id == requestId && x.RequestedByEmail == trimmedEmail)
            .FirstOrDefaultAsync(cancellationToken);

        return request is null ? null : MapPortalStatus(request);
    }

    public async Task<PortalTenantRequestStatusDto?> GetPortalStatusByTenantIdAsync(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        var request = await adminDbContext.TenantCreationRequests
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.ProcessedAtUtc ?? x.RequestedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return request is null ? null : MapPortalStatus(request);
    }

    public async Task<PortalTenantRequestStatusDto?> GetLatestPortalStatusForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var request = await adminDbContext.TenantCreationRequests
            .AsNoTracking()
            .Where(x => x.RequestedByUserId == userId)
            .OrderByDescending(x => x.ProcessedAtUtc ?? x.RequestedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return request is null ? null : MapPortalStatus(request);
    }

    public async Task<IReadOnlyList<TenantRequestListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => (await adminDbContext.TenantCreationRequests
            .AsNoTracking()
            .OrderByDescending(x => x.RequestedAtUtc)
            .ToListAsync(cancellationToken))
            .Select(Map)
            .ToList();

    public async Task<TenantRequestListItemDto?> ApproveAsync(ProcessTenantRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await adminDbContext.TenantCreationRequests.FirstOrDefaultAsync(x => x.Id == command.RequestId, cancellationToken);
        if (request is null)
            return null;

        var user = await userManager.FindByIdAsync(request.RequestedByUserId);
        if (user is null)
            throw new InvalidOperationException("Requesting user was not found.");

        var tenantNameInUse = await adminDbContext.Tenants
            .AnyAsync(x => x.Name == request.TenantName && x.Id != request.TenantId, cancellationToken);
        if (tenantNameInUse)
            throw new InvalidOperationException("A tenant with the requested name already exists.");

        var tenant = new Tenant(
            request.TenantName,
            null,
            new Contact(
                $"{request.RequestedByFirstName} {request.RequestedByLastName}".Trim(),
                request.RequestedByEmail,
                user.PhoneNumber ?? "+37120000000",
                new Address("Unknown", "LV-0001", "Riga", "Latvia"),
                "https://callio.local"),
            DateTime.UtcNow,
            DateTime.UtcNow)
        {
            Name = request.TenantName
        };

        adminDbContext.Tenants.Add(tenant);
        await adminDbContext.SaveChangesAsync(cancellationToken);

        var selectedPlanId = await ResolveRequestedPlanIdAsync(request, cancellationToken);
        if (selectedPlanId.HasValue)
        {
            var now = DateTime.UtcNow;
            var subscription = new Subscription(
                tenant.Id,
                selectedPlanId.Value,
                new DateRange(now, now.AddMonths(1), now));

            adminDbContext.Subscriptions.Add(subscription);
            await adminDbContext.SaveChangesAsync(cancellationToken);
        }

        request.Approve(tenant.Id, command.ProcessedByUserId, command.DecisionNote, DateTime.UtcNow);
        await adminDbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await publishEndpoint.Publish(
                new TenantApprovedIntegrationEvent(request.RequestedByUserId, tenant.Id, request.Id),
                cancellationToken);
        }
        catch
        {
            user.LinkToTenant(tenant.Id);
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join("; ", updateResult.Errors.Select(x => x.Description));
                throw new InvalidOperationException($"Tenant approval succeeded but user linking failed: {errors}");
            }
        }

        return Map(request);
    }

    public async Task<TenantRequestListItemDto?> RejectAsync(ProcessTenantRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await adminDbContext.TenantCreationRequests.FirstOrDefaultAsync(x => x.Id == command.RequestId, cancellationToken);
        if (request is null)
            return null;

        request.Reject(command.ProcessedByUserId, command.DecisionNote, DateTime.UtcNow);
        await adminDbContext.SaveChangesAsync(cancellationToken);

        return Map(request);
    }

    private static TenantRequestListItemDto Map(TenantCreationRequest request)
    {
        var selectedPlan = ExtractSelectedPlan(request.Notes);

        return new TenantRequestListItemDto(
            request.Id,
            request.TenantName,
            request.CompanyName,
            request.RequestedByEmail,
            $"{request.RequestedByFirstName} {request.RequestedByLastName}".Trim(),
            request.Status,
            request.RequestedAtUtc,
            request.ProcessedAtUtc,
            request.Notes,
            request.DecisionNote,
            request.ProcessedByUserId,
            request.TenantId,
            selectedPlan.PlanId,
            selectedPlan.PlanName);
    }

    private static PortalTenantRequestStatusDto MapPortalStatus(TenantCreationRequest request)
    {
        var selectedPlan = ExtractSelectedPlan(request.Notes);

        return new PortalTenantRequestStatusDto(
            request.Id,
            request.TenantName,
            request.CompanyName,
            request.RequestedByEmail,
            $"{request.RequestedByFirstName} {request.RequestedByLastName}".Trim(),
            request.Status.ToString(),
            request.RequestedAtUtc,
            request.ProcessedAtUtc,
            request.DecisionNote,
            request.TenantId,
            selectedPlan.PlanId,
            selectedPlan.PlanName);
    }

    private async Task EnsureCanSubmitRequestAsync(string email, string tenantName, CancellationToken cancellationToken)
    {
        var trimmedEmail = email.Trim();
        var trimmedTenantName = tenantName.Trim();

        var hasPendingRequest = await adminDbContext.TenantCreationRequests
            .AnyAsync(x => x.RequestedByEmail == trimmedEmail && x.Status == TenantRequestStatus.Pending, cancellationToken);
        if (hasPendingRequest)
            throw new InvalidOperationException("There is already a pending tenant request for this email address.");

        var tenantNameTaken = await adminDbContext.Tenants
            .AnyAsync(x => x.Name == trimmedTenantName, cancellationToken);
        if (tenantNameTaken)
            throw new InvalidOperationException("The requested tenant name is already in use.");
    }

    private async Task<int?> ResolveRequestedPlanIdAsync(TenantCreationRequest request, CancellationToken cancellationToken)
    {
        var selectedPlan = ExtractSelectedPlan(request.Notes);
        if (selectedPlan.PlanId.HasValue)
        {
            var exists = await adminDbContext.Plans.AnyAsync(x => x.Id == selectedPlan.PlanId.Value, cancellationToken);
            if (exists)
                return selectedPlan.PlanId.Value;
        }

        if (!string.IsNullOrWhiteSpace(selectedPlan.PlanName))
        {
            var planId = await adminDbContext.Plans
                .Where(x => x.Name == selectedPlan.PlanName)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (planId.HasValue)
                return planId.Value;
        }

        return null;
    }

    private async Task<SelectedPlanDetails?> ResolveSelectedPlanAsync(int? selectedPlanId, CancellationToken cancellationToken)
    {
        if (!selectedPlanId.HasValue)
            return null;

        var selectedPlan = await adminDbContext.Plans
            .AsNoTracking()
            .Where(x => x.Id == selectedPlanId.Value && x.IsActive)
            .Select(x => new SelectedPlanDetails(x.Id, x.Name))
            .FirstOrDefaultAsync(cancellationToken);

        if (selectedPlan is null)
            throw new InvalidOperationException("The selected subscription plan is no longer available.");

        return selectedPlan;
    }

    private static string? BuildStoredNotes(string? notes, SelectedPlanDetails? selectedPlan)
    {
        var trimmedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (selectedPlan is null)
            return trimmedNotes;

        var selectionHeader = string.Join(
            Environment.NewLine,
            $"Selected subscription plan id: {selectedPlan.Id}",
            $"Selected subscription plan: {selectedPlan.Name}");

        return string.IsNullOrWhiteSpace(trimmedNotes)
            ? selectionHeader
            : $"{selectionHeader}{Environment.NewLine}{Environment.NewLine}{trimmedNotes}";
    }

    private static SelectedPlanDetails ExtractSelectedPlan(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return SelectedPlanDetails.Empty;

        int? planId = null;
        string? planName = null;

        var idMatch = Regex.Match(notes, @"^Selected subscription plan id:\s*(\d+)\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        if (idMatch.Success && int.TryParse(idMatch.Groups[1].Value, out var parsedId))
            planId = parsedId;

        var nameMatch = Regex.Match(notes, @"^Selected subscription plan:\s*(.+)\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        if (nameMatch.Success)
            planName = nameMatch.Groups[1].Value.Trim();

        return new SelectedPlanDetails(planId, planName);
    }

    private sealed record SelectedPlanDetails(int? Id, string? Name)
    {
        public static readonly SelectedPlanDetails Empty = new(null, null);

        public int? PlanId => Id;
        public string? PlanName => Name;
    }
}

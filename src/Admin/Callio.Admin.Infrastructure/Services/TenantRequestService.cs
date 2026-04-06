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

        var existingUser = await userManager.FindByEmailAsync(command.Email);
        if (existingUser is not null)
            throw new InvalidOperationException("A user with this email already exists.");

        var user = ApplicationUser.CreatePowerUser(command.Email, command.FirstName, command.LastName);
        var createUserResult = await userManager.CreateAsync(user, command.Password);
        if (!createUserResult.Succeeded)
        {
            var errors = string.Join("; ", createUserResult.Errors.Select(x => x.Description));
            throw new InvalidOperationException($"Portal user registration failed: {errors}");
        }

        try
        {
            var request = new TenantCreationRequest(
                command.TenantName,
                user.Id,
                command.Email,
                command.FirstName,
                command.LastName,
                command.CompanyName,
                command.Notes,
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
                "Portal user registered and tenant request submitted for Callio dashboard review.");
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

        return await adminDbContext.TenantCreationRequests
            .AsNoTracking()
            .Where(x => x.Id == requestId && x.RequestedByEmail == trimmedEmail)
            .Select(x => new PortalTenantRequestStatusDto(
                x.Id,
                x.TenantName,
                x.CompanyName,
                x.RequestedByEmail,
                $"{x.RequestedByFirstName} {x.RequestedByLastName}".Trim(),
                x.Status.ToString(),
                x.RequestedAtUtc,
                x.ProcessedAtUtc,
                x.DecisionNote,
                x.TenantId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TenantRequestListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => await adminDbContext.TenantCreationRequests
            .AsNoTracking()
            .OrderByDescending(x => x.RequestedAtUtc)
            .Select(x => new TenantRequestListItemDto(
                x.Id,
                x.TenantName,
                x.CompanyName,
                x.RequestedByEmail,
                $"{x.RequestedByFirstName} {x.RequestedByLastName}".Trim(),
                x.Status,
                x.RequestedAtUtc,
                x.ProcessedAtUtc,
                x.Notes,
                x.DecisionNote,
                x.ProcessedByUserId,
                x.TenantId))
            .ToListAsync(cancellationToken);

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
        => new(
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
            request.TenantId);

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
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            var idMatch = Regex.Match(request.Notes, @"Selected subscription plan id:\s*(\d+)", RegexOptions.IgnoreCase);
            if (idMatch.Success && int.TryParse(idMatch.Groups[1].Value, out var parsedId))
            {
                var exists = await adminDbContext.Plans.AnyAsync(x => x.Id == parsedId, cancellationToken);
                if (exists)
                    return parsedId;
            }

            var nameMatch = Regex.Match(request.Notes, @"Selected subscription plan:\s*(.+)", RegexOptions.IgnoreCase);
            if (nameMatch.Success)
            {
                var planName = nameMatch.Groups[1].Value.Trim();
                var planId = await adminDbContext.Plans
                    .Where(x => x.Name == planName)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (planId.HasValue)
                    return planId.Value;
            }
        }

        return null;
    }
}

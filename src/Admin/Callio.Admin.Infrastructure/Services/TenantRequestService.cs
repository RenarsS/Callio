using Callio.Admin.Application.Tenants;
using Callio.Admin.Domain;
using Callio.Admin.Domain.ValueObjects;
using Callio.Admin.Infrastructure.Persistence;
using Callio.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Callio.Admin.Infrastructure.Services;

public class TenantRequestService(
    AdminDbContext adminDbContext,
    UserManager<ApplicationUser> userManager) : ITenantRequestService
{
    public async Task<TenantRequestListItemDto> CreateAsync(CreateTenantRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = new TenantCreationRequest(
            command.TenantName,
            command.RequestedByUserId,
            command.RequestedByEmail,
            command.RequestedByFirstName,
            command.RequestedByLastName,
            command.CompanyName,
            command.Notes,
            DateTime.UtcNow);

        adminDbContext.TenantCreationRequests.Add(request);
        await adminDbContext.SaveChangesAsync(cancellationToken);

        return Map(request);
    }

    public async Task<IReadOnlyList<TenantRequestListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => await adminDbContext.TenantCreationRequests
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
                x.DecisionNote,
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

        var tenant = new Tenant(
            request.TenantName,
            null,
            new Contact($"{request.RequestedByFirstName} {request.RequestedByLastName}".Trim(), request.RequestedByEmail, user.PhoneNumber ?? "+37120000000", new Address("Unknown", "LV-0001", "Riga", "Latvia"), "https://callio.local"),
            DateTime.UtcNow,
            DateTime.UtcNow)
        {
            Name = request.TenantName
        };

        adminDbContext.Tenants.Add(tenant);
        await adminDbContext.SaveChangesAsync(cancellationToken);

        user.LinkToTenant(tenant.Id);
        await userManager.UpdateAsync(user);

        request.Approve(tenant.Id, command.ProcessedByUserId, command.DecisionNote, DateTime.UtcNow);
        await adminDbContext.SaveChangesAsync(cancellationToken);

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
            request.DecisionNote,
            request.TenantId);
}

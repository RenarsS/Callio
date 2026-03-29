using Callio.Admin.Domain.Enums;

namespace Callio.Admin.Application.Tenants;

public record CreateTenantRequestCommand(
    string TenantName,
    string RequestedByUserId,
    string RequestedByEmail,
    string RequestedByFirstName,
    string RequestedByLastName,
    string CompanyName,
    string? Notes);

public record ProcessTenantRequestCommand(
    int RequestId,
    string ProcessedByUserId,
    string? DecisionNote);

public record TenantRequestListItemDto(
    int Id,
    string TenantName,
    string CompanyName,
    string RequestedByEmail,
    string RequestedByFullName,
    TenantRequestStatus Status,
    DateTime RequestedAtUtc,
    DateTime? ProcessedAtUtc,
    string? DecisionNote,
    int? TenantId);
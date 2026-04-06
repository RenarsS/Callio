using Callio.Admin.Domain.Enums;

namespace Callio.Admin.Application.Tenants;

public record RegisterPortalUserAndTenantCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string CompanyName,
    string TenantName,
    int? SelectedPlanId,
    string? Notes);

public record PortalTenantOnboardingResultDto(
    string UserId,
    int TenantRequestId,
    string Email,
    string TenantName,
    string CompanyName,
    string Status,
    string Message,
    int? SelectedPlanId,
    string? SelectedPlanName);

public record PortalTenantRequestStatusDto(
    int Id,
    string TenantName,
    string CompanyName,
    string RequestedByEmail,
    string RequestedByFullName,
    string Status,
    DateTime RequestedAtUtc,
    DateTime? ProcessedAtUtc,
    string? DecisionNote,
    int? TenantId,
    int? SelectedPlanId,
    string? SelectedPlanName);

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
    string? Notes,
    string? DecisionNote,
    string? ProcessedByUserId,
    int? TenantId,
    int? SelectedPlanId,
    string? SelectedPlanName);

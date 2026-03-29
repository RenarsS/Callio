using Callio.Admin.Domain.Enums;
using Callio.Core.Domain.Exceptions;
using Callio.Core.Domain.Helpers;

namespace Callio.Admin.Domain;

public class TenantCreationRequest : Entity<int>
{
    public string TenantName { get; private set; } = string.Empty;
    
    public string RequestedByUserId { get; private set; } = string.Empty;
    
    public string RequestedByEmail { get; private set; } = string.Empty;
    
    public string RequestedByFirstName { get; private set; } = string.Empty;
    
    public string RequestedByLastName { get; private set; } = string.Empty;
    
    public string CompanyName { get; private set; } = string.Empty;
    
    public string? Notes { get; private set; }
    
    public TenantRequestStatus Status { get; private set; }
    
    public DateTime RequestedAtUtc { get; private set; }
    
    public DateTime? ProcessedAtUtc { get; private set; }
    
    public string? ProcessedByUserId { get; private set; }
    
    public string? DecisionNote { get; private set; }
    
    public int? TenantId { get; private set; }

    private TenantCreationRequest() { }

    public TenantCreationRequest(
        string tenantName,
        string requestedByUserId,
        string requestedByEmail,
        string requestedByFirstName,
        string requestedByLastName,
        string companyName,
        string? notes,
        DateTime requestedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(tenantName))
            throw new InvalidFieldException(nameof(TenantName));
        
        if (string.IsNullOrWhiteSpace(requestedByUserId))
            throw new InvalidFieldException(nameof(RequestedByUserId));
        
        if (string.IsNullOrWhiteSpace(requestedByEmail))
            throw new InvalidFieldException(nameof(RequestedByEmail));
        
        if (string.IsNullOrWhiteSpace(companyName))
            throw new InvalidFieldException(nameof(CompanyName));

        TenantName = tenantName.Trim();
        RequestedByUserId = requestedByUserId.Trim();
        RequestedByEmail = requestedByEmail.Trim();
        RequestedByFirstName = requestedByFirstName.Trim();
        RequestedByLastName = requestedByLastName.Trim();
        CompanyName = companyName.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        RequestedAtUtc = requestedAtUtc;
        Status = TenantRequestStatus.Pending;
    }

    public void Approve(int tenantId, string processedByUserId, string? decisionNote, DateTime processedAtUtc)
    {
        EnsurePending();
        TenantId = tenantId;
        Status = TenantRequestStatus.Approved;
        ProcessedByUserId = processedByUserId;
        ProcessedAtUtc = processedAtUtc;
        DecisionNote = string.IsNullOrWhiteSpace(decisionNote) ? null : decisionNote.Trim();
    }

    public void Reject(string processedByUserId, string? decisionNote, DateTime processedAtUtc)
    {
        EnsurePending();
        Status = TenantRequestStatus.Rejected;
        ProcessedByUserId = processedByUserId;
        ProcessedAtUtc = processedAtUtc;
        DecisionNote = string.IsNullOrWhiteSpace(decisionNote) ? null : decisionNote.Trim();
    }

    private void EnsurePending()
    {
        if (Status != TenantRequestStatus.Pending)
            throw new InvalidOperationException("Only pending tenant requests can be processed.");
    }
}

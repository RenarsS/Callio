using Callio.Admin.Domain.Enums;
using Callio.Admin.Domain.Exceptions.Tenant;
using Callio.Admin.Domain.ValueObjects;
using Callio.Core.Domain.Exceptions;
using Callio.Core.Domain.Helpers;

namespace Callio.Admin.Domain;

public class Tenant : Entity<int>
{
    public required string Name { get; set; }
    
    public Guid? TenantCode { get; set; }
    
    public Contact Contact { get; set; }

    public DateTime CreatedAt { get; set; }
    
    public DateTime ActivatedAt { get; set; }

    public DateTime? DeactivatedAt { get; set; }

    public TenancyStatus Status { get; set; }
    
    private Tenant() { }

    public Tenant(string name, Guid? tenantCode, Contact contact,
        DateTime createdAt, DateTime activatedAt, DateTime? deactivatedAt = null, DateTime? now = null) 
        : this(name, tenantCode, contact, createdAt, activatedAt, new TenancyStatus(now, activatedAt, deactivatedAt), deactivatedAt, now)
    {
    }

    private Tenant(string name, 
        Guid? tenantCode, 
        Contact contact,
        DateTime createdAt, 
        DateTime activatedAt, 
        TenancyStatus status,
        DateTime? deactivatedAt = null,
        DateTime? now = null)
    {
        if (string.IsNullOrEmpty(name))
            throw new InvalidFieldException(nameof(Name));
        
        if (createdAt > activatedAt)
            throw new InvalidDateException("Tenant can't be activated before creation date.");
        
        if (deactivatedAt < activatedAt)
            throw new InvalidDateException("Tenant can't be deactivated before activation date.");
            
        Name = name;
        TenantCode = tenantCode ?? Guid.NewGuid();
        Contact = contact;
        CreatedAt = createdAt;
        ActivatedAt = activatedAt;
        DeactivatedAt = deactivatedAt;
        Status = status;
    }
}
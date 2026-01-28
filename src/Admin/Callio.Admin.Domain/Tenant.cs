using Callio.Admin.Domain.Enums;
using Callio.Core.Domain.Helpers;

namespace Callio.Admin.Domain;

public class Tenant : Entity<int>
{
    public required string Name { get; set; }
    
    public Guid TenantCode { get; set; }

    public int CompanyId { get; set; }

    public DateTime CreatedAt { get; set; }
    
    public DateTime ActivatedAt { get; set; }

    public DateTime DeactivatedAt { get; set; }

    public Status Status { get; set; }
}
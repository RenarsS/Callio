using Callio.Admin.Domain.ValueObjects;
using Callio.Core.Domain.Helpers;

namespace Callio.Admin.Domain;

public class Company : Entity<int>
{
    public required string Name { get; set; }

    public required string VatNumber { get; set; }

    public required Contact Contact { get; set; }
}
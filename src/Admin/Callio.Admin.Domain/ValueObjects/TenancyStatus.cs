using Callio.Admin.Domain.Enums;
using Callio.Admin.Domain.Exceptions.Tenant;

namespace Callio.Admin.Domain.ValueObjects;

public record TenancyStatus
{
    public Status Value { get; private set; }

    public TenancyStatus(DateTime? today, DateTime activationDate, DateTime? deactivationDate = null)
    {
        today ??= DateTime.Now;

        if (activationDate >= deactivationDate)
            throw new InvalidDateException("Deactivation date must be after activation date.");

        if (today < activationDate)
        {
            Value = Status.Pending;
        }

        if (today >= activationDate)
        {
            Value = Status.Enabled;
        }

        if (today >= deactivationDate)
        {
            Value = Status.Disabled;
        }
    }
}
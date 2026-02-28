using Callio.Admin.Domain.Enums;

namespace Callio.Admin.Domain.ValueObjects;

public record BillingCycle(BillingInterval Interval, int AnchorDay);

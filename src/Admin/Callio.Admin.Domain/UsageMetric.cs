using Callio.Admin.Domain.Enums;
using Callio.Core.Domain.Helpers;

namespace Callio.Admin.Domain;

public class UsageMetric(string key, string displayName, string unit, MeasurementType type)
    : Entity<int>
{
    public string Key { get; private set; } = key;

    public string DisplayName { get; private set; } = displayName;

    public string Unit { get; private set; } = unit;

    public MeasurementType Type { get; private set; } = type;
}
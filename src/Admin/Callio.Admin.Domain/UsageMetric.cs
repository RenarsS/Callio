using Callio.Admin.Domain.Enums;
using Callio.Core.Domain.Helpers;

namespace Callio.Admin.Domain;

public class UsageMetric : Entity<int>
{
    public string Key { get; private set; }

    public string DisplayName { get; private set; }

    public string Unit { get; private set; }

    public MeasurementType Type { get; private set; }

    private UsageMetric() { }
    
    public UsageMetric(string key, string displayName, string unit, MeasurementType type)
    {
        Key = key;
        DisplayName = displayName;
        Unit = unit;
        Type = type;
    }
}
using Callio.Admin.Domain;
using Callio.Admin.Domain.Enums;
using FluentAssertions;

namespace Callio.Admin.Tests.Domain;

public class UsageMetricTests
{
    [Fact]
    public void UsageMeasurement_AllFieldsAreValid_FieldsAreSet()
    {
        // Act
        var usageMetric = new UsageMetric("Key", "DisplayName", "Unit", MeasurementType.Cumulative);

        // Assert
        usageMetric.Key.Should().Be("Key");
        usageMetric.DisplayName.Should().Be("DisplayName");
        usageMetric.Unit.Should().Be("Unit");
        usageMetric.Type.Should().Be(MeasurementType.Cumulative);
    }
}
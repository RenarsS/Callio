using Callio.Admin.Domain;
using FluentAssertions;

namespace Callio.Admin.Tests.Domain;

public class UsageRecordTests
{
    [Fact]
    public void UsageRecord_AllFieldsAreValid_FieldsAreSet()
    {
        // Act
        var usageRecord = new UsageRecord(1, 1, 5, new DateTime(2026, 2, 28), "Reference");

        // Assert
        usageRecord.TenantId.Should().Be(1);
        usageRecord.UsageMetricId.Should().Be(1);
        usageRecord.Quantity.Should().Be(5);
        usageRecord.RecordedAt.Should().Be(new DateTime(2026, 2, 28));
        usageRecord.SourceReference.Should().Be("Reference");
    }
}
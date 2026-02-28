using Callio.Admin.Domain.ValueObjects;
using FluentAssertions;

namespace Callio.Admin.Tests.Domain.ValueObjects;

public class DateRangeTests
{
    private static readonly DateTime Start = new(2026, 1, 1);
    private static readonly DateTime End = new(2026, 3, 1);
    private static readonly DateTime Now = new(2026, 2, 13);

    private static readonly DateTime BeforeRange = new (2025, 12, 30);
    private static readonly DateTime AfterRange = new (2026, 3, 2);

    private static readonly DateRange DateRange = new (Start, End, Now);
    
    [Fact]
    public void Contains_DateWithinRange_ReturnsTrue()
    {
        // Act
        var result = DateRange.Contains(Now);

        // Assert
        result.Should().BeTrue();
    }
    
    [Fact]
    public void Contains_DateBeforeRange_ReturnsFalse()
    {
        // Act
        var result = DateRange.Contains(BeforeRange);

        // Assert
        result.Should().BeFalse();
    }
    
    [Fact]
    public void Contains_DateAfterRange_ReturnsFalse()
    {
        // Act
        var result = DateRange.Contains(AfterRange);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_DateWithinRange_ReturnsFalse()
    {
        // Act
        var result = DateRange.IsExpired();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_DateAfterRange_ReturnsTrue()
    {
        // Arrange
        var dateRange = new DateRange(Start, End, AfterRange);
        
        // Act
        var result = dateRange.IsExpired();

        // Assert
        result.Should().BeTrue();
    }
}
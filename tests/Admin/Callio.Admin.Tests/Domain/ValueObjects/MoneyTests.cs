using Callio.Admin.Domain.ValueObjects;
using FluentAssertions;

namespace Callio.Admin.Tests.Domain.ValueObjects;

public class MoneyTests
{
    private const string Euro = "EUR";
    private const string Dollar = "USD";

    private static readonly Money Euros = new(20, Euro);
    private static readonly Money Dollars = new(20, Dollar);
    
    [Fact]
    public void Add_CorrectCurrency_NewResultReturned()
    {
        // Act
        var result = Euros.Add(Euros);
        
        // Assert
        result.Amount.Should().Be(40);
        result.Currency.Should().Be(Euro);
    }

    [Fact]
    public void Add_IncorrectCurrency_ExceptionThrown()
    {
        // Act
        var act = () => Euros.Add(Dollars);
        
        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("Currency mismatch.");
    }

    [Theory]
    [InlineData(2, 40)]
    [InlineData(0.5, 10)]
    [InlineData(0, 0)]
    public void Multiply_DifferentFactor_CorrectValues(decimal factor, decimal expected)
    {
        // Act
        var result = Euros.Multiply(factor);
        
        // Assert
        result.Amount.Should().Be(expected);
        result.Currency.Should().Be(Euro);
    }
}
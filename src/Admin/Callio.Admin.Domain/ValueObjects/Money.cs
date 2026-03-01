namespace Callio.Admin.Domain.ValueObjects;

public record Money
{
    public decimal Amount { get; set; }

    public string Currency { get; set; }
    
    private Money() { }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }
    
    public Money Add(Money other)
    {
        if (Currency != other.Currency) throw new InvalidOperationException("Currency mismatch.");
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => new(Amount * factor, Currency);
}
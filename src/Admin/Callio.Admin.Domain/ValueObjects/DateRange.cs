namespace Callio.Admin.Domain.ValueObjects;

public record DateRange(DateTime Start, DateTime End, DateTime Now)
{
    public bool Contains(DateTime date) => date >= Start && date <= End;
    
    public bool IsExpired() => End < Now;
}
namespace Callio.Admin.Domain.ValueObjects;

public record DateRange
{
    public DateTime Start { get; set; }

    public DateTime End { get; set; }

    public DateTime Now{ get; set; }
    
    public bool Contains(DateTime date) => date >= Start && date <= End;
    
    public bool IsExpired() => End < Now;
    
    private DateRange() { }

    public DateRange(DateTime start, DateTime end, DateTime now)
    {
        Start = start;
        End = end;
        Now = now;
    }
}
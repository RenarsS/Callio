namespace Callio.Core.Domain.Helpers;

public abstract class DomainEvent
{
    public Guid Id { get; protected set; }

    public DateTime OccurredOn { get; protected set; }

    protected DomainEvent()
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTime.Now;       
    }

    protected DomainEvent(Guid id, DateTime occurredOn)
    {
        Id = id;
        OccurredOn = occurredOn;       
    }
}
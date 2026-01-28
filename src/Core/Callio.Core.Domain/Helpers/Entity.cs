namespace Callio.Core.Domain.Helpers;

public abstract class Entity<TId>
{
    public TId Id { get; protected set; }

    public override bool Equals(object obj) 
        => obj is Entity<TId> entity && EqualityComparer<TId>.Default.Equals(Id, entity.Id);
    
    public override int GetHashCode() 
        => Id?.GetHashCode() ?? 0;
}
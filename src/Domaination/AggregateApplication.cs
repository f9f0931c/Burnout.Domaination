using System;
namespace Burnout.Domaination; 

public sealed class AggregateApplication<TEvent>
    where TEvent : IDomainEvent
{
    public TEvent Event { get; }

    internal AggregateApplication(
        TEvent @event) 
    {
        Event = @event ?? throw new ArgumentNullException(nameof(@event));   
    }
}

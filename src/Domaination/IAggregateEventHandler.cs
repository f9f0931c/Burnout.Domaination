namespace Burnout.Domaination; 

public interface IAggregateEventHandler<TEvent>
    where TEvent : IDomainEvent {
    void Handle(AggregateApplication<TEvent> @event);
}

using System;

namespace Burnout.Domaination; 

public class StagedEvent {
    public IDomainEvent Event { get; }

    internal StagedEvent(IDomainEvent @event) {
        Event = @event ?? throw new ArgumentNullException(nameof(@event));
    }
}

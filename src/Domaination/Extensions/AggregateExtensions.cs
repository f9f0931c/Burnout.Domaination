using System;
using System.Collections.Generic;
using System.Linq;
using Burnout.Domaination;

namespace Domaination {
    public static class AggregateExtensions {
        public static void Apply<TIdentity>(
            this Aggregate<TIdentity> aggregate,
            IEnumerable<IDomainEvent> events) 
        {
            _ = events ?? throw new ArgumentNullException(nameof(events));
            foreach (var @event in @events) aggregate.Apply(@event);
        }

        public static void Apply<TIdentity>(
            this Aggregate<TIdentity> aggregate,
            params IDomainEvent[] events) =>
            aggregate.Apply(events.AsEnumerable());
    }
}

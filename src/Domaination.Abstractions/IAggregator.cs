using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Burnout.Domaination;

public interface IAggregator<TKey, TModel>
    where TKey : struct
{
    Task EmitAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
}

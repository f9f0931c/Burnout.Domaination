using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Burnout.Domaination;

public interface IAggregationHydrator<TKey, TModel>
    where TKey : struct
{
    Task<IAggregator<TKey, TModel>> HydrateAsync(string snapshot, IEnumerable<IDomainEvent> @events, CancellationToken cancellationToken);
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Burnout.Domaination;

public interface IAggregationStorage<TKey, TModel>
    where TKey : struct
{
    Task<IAggregator<TKey, TModel>> HydrateAsync(TKey key, IAggregationHydrator<TKey, TModel> hydrator, CancellationToken cancellationToken);
}

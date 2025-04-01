using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Burnout.Domaination;

public interface IAggregationProvider
{
    Task<IAggregator<TKey, TModel>> Create<TKey, TModel>(TKey key, TModel model, CancellationToken cancellationToken)
        where TKey : struct;

    Task<IAggregator<TKey, TModel>> GetAsync<TKey, TModel>(TKey key, IAggregationStorage<TKey, TModel> storage, CancellationToken cancellationToken)
        where TKey : struct; 
}

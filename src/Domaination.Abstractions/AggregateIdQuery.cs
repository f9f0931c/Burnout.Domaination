using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Burnout.Domaination;

interface IAggregateLookupProvider<TContext>
{
    Task<IAggregateLookup<TContext, TModel>> CreateAsync<TModel>(CancellationToken cancellationToken);
}

interface IAggregateLookup<TContext, TModel>
{
    Task ExecuteAsync(TContext context, CancellationToken cancellationToken = default);
}

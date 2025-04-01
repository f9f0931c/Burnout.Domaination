using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Burnout.Domaination;

public interface IEventApplicator
{
    Task ApplyAsync<TModel, TEvent>(TModel model, TEvent @event, CancellationToken cancellationToken);
}

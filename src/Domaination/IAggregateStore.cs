using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Burnout.Domaination; 

public interface IAggregateStore
{
    Task CommitAsync(IEnumerable<StagedEvent> events, CancellationToken cancellationToken);
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Burnout.Domaination;

public interface IModelSerializer
{
    Task<string> SerializeAsync<TModel>(TModel model, CancellationToken cancellationToken = default);
    Task<TModel> DeserializeAsync<TModel>(string serialized, CancellationToken cancellationToken = default);
}

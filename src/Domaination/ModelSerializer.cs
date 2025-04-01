using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Burnout.Domaination; 

class ModelSerializer : IModelSerializer
{
    public Task<string> SerializeAsync<TModel>(TModel model, CancellationToken cancellationToken = default) 
    {
        return Task.FromResult(JsonSerializer.Serialize<TModel>(model));
    }

    public Task<TModel> DeserializeAsync<TModel>(string serialized, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(JsonSerializer.Deserialize<TModel>(serialized));
    }
}

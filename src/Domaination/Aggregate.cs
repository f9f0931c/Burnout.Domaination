using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Burnout.Domaination;

public interface IAggregate<TIdentity>
{
    TIdentity Id { get; }
    int Version { get; }
    void Emit<TEvent>(TEvent @event)
        where TEvent: IDomainEvent;
    Task CommitAsync(IAggregateStore aggregateStore, CancellationToken cancellationToken);
}

public class Aggregate2<TIdentity> : IAggregate<TIdentity>
{
    public TIdentity Id { get; private set; }
    public int Version { get; private set; } = 0;
    readonly List<IDomainEvent> Events = new List<IDomainEvent>();
    public void Emit<TEvent>(TEvent @event)
        where TEvent : IDomainEvent 
    {
        Events.Add(@event);
    }

    public void Rehydrate(IEnumerable<IDomainEvent> events, Action<IDomainEvent> applicator) {
        foreach (var @event in events) {
            // Check order and identifier
            applicator(@event);
            Version++;
        }
    } 

    public async Task CommitAsync(IAggregateStore aggregateStore, CancellationToken cancellationToken) {
        await aggregateStore.CommitAsync(
            Events.Select(@event => new StagedEvent(@event)), cancellationToken)
            .ConfigureAwait(false);
        Events.Clear();
        Version++;
    }
}

class TestEvent : IDomainEvent {
    public TestEvent(Guid id) {

    }
}

class Model {
    public void Apply(TestEvent @event) {

    }
}

public class ApplicationStrategy {
    readonly ConcurrentDictionary<Type, IReadOnlyDictionary<Type, MethodInfo>> Applicators =
        new ConcurrentDictionary<Type, IReadOnlyDictionary<Type, MethodInfo>>();

    IReadOnlyDictionary<Type, MethodInfo> LoadApplicators(Type type) {
        return Applicators.GetOrAdd(type, type => type
            .GetTypeInfo()
            .GetDeclaredMethods("Apply")
            .Select(method => new { method, parameters = method.GetParameters() })
            .Where(x => x.parameters.Length == 1)
            .ToDictionary(
                x => x.parameters
                    .Single()
                    .ParameterType,
                x => x.method
            ));
    }
    public void Apply<TModel, TEvent>(TModel model, IntegrationEvent<TEvent> @event)
        where TEvent : IDomainEvent
    {
        if (model is IAggregateEventHandler2<TEvent> handler)
            handler.Apply(@event);
    }

    public void Apply<TModel>(TModel model, IDomainEvent @event) {
        var applicators = LoadApplicators(typeof(TModel));
        if (!applicators.TryGetValue(@event.GetType(), out var applicator))
            throw new Exception();
        applicator.Invoke(model, new object[] { @event });
    }
}

public interface IAggregateEventHandler2<TEvent> 
    where TEvent : IDomainEvent
{
    void Apply(IntegrationEvent<TEvent> @event);
}

public interface IApplicationStrategy {
    void Apply<TModel, TEvent>(TModel model, IntegrationEvent<TEvent> @event)
        where TEvent : IDomainEvent;
}

public class Applicator2<TModel, TEvent>
    where TEvent : IDomainEvent
{
    readonly FunctionalApplicatorStrategy strategy;
    readonly IntegrationEvent<TEvent> Event;

    public void Apply(TModel model) {
        strategy.Apply(model, Event);
    }
}

public class FunctionalApplicatorStrategy : IApplicationStrategy
{
    public void Apply<TModel, TEvent>(TModel model, IntegrationEvent<TEvent> @event)
        where TEvent : IDomainEvent
    {
        if (model is IAggregateEventHandler2<TEvent> handler)
            handler.Apply(@event);
    }
}
class Test {
    public Model Model { get; }
    public IAggregate<Guid> Aggregate { get; }

    public void DoSomething(Guid id) {
        var @event = new TestEvent(id);
        var strat = new ApplicationStrategy();
        Model.Apply(@event);
        Aggregate.Emit(@event);
    }
}

class Testing {
    void Test() {
        var test = new Test();
        test.DoSomething(Guid.NewGuid());
    }
}

public class Aggregate<TIdentity> {
    public int Version { get; private set; } = 0;
    readonly List<IDomainEvent> Events = new List<IDomainEvent>();

    protected void Emit<TEvent>(TEvent @event) 
        where TEvent : IDomainEvent
    {
        Events.Add(@event);
    }

    public async Task CommitAsync(IAggregateStore aggregateStore, CancellationToken cancellationToken) {
        await aggregateStore.CommitAsync(
            Events.Select(@event => new StagedEvent(@event)), 
            cancellationToken);
        Events.Clear();
        Version++;
    }
}

interface IDomainEventApplicator
{
    void Apply<TModel>(TModel model, IDomainEvent @event);
}

interface IDomainEventApplicator<TModel, TEvent>
{
    void Apply(TModel model, TEvent @event);
}
interface IDomainEventApplicator<TModel> 
{
    
}

interface InnerDomainEventApplicator<TModel>
{
    void Apply(IDomainEventApplicator<TModel> applicator, TModel model, IDomainEvent @event);
}

class InnerApplicator<TModel, TEvent> : InnerDomainEventApplicator<TModel>
{
    public void Apply(IDomainEventApplicator<TModel> applicator, TModel model, IDomainEvent @event)
    {
        if (applicator is IDomainEventApplicator<TModel, TEvent> specialized)
            specialized.Apply(model, (TEvent)@event);
    }
}

class DomainEventModelApplicator : IDomainEventApplicator
{
    readonly IServiceProvider Services;

    public DomainEventModelApplicator(
        IServiceProvider services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public void Apply<TModel>(TModel model, IDomainEvent @event)
    {
        IDomainEventApplicator<TModel> handler = null;
        var applicator = (InnerDomainEventApplicator<TModel>)
            Activator.CreateInstance(
                typeof(InnerApplicator<,>)
                    .MakeGenericType(
                        typeof(TModel),
                        @event.GetType()));
        applicator.Apply(handler, model, @event);
    }
}

public class IntegrationEvent<TKey, TEvent>
{
    public Guid CommitId { get; }
    public Guid Id { get; }
    public Guid PreviousId { get; }
    public Guid NextId { get; }
    public DateTimeOffset Timestamp { get; }
    public TKey AggregateKey { get; }
    public TEvent DomainEvent { get; }
}

interface IMaterialization<TKey, TModel>
{
    TKey Key { get; }
    TModel Model { get; }
    void Emit<TEvent>(TEvent @event) where TEvent : IDomainEvent;
}

interface IMaterializer<TKey, TModel>
    where TKey : struct
{
    IMaterialization<TKey, TModel> Materialize(
        Tracker<TKey, TModel> tracker,
        TModel model);
}

class Materializer<TKey, TModel> : IMaterializer<TKey, TModel>
    where TKey : struct
{
    readonly IDomainEventApplicator Applicator;
    public Materializer(
        IDomainEventApplicator applicator)
    {
        Applicator = applicator ?? throw new ArgumentNullException(nameof(applicator));
    } 

    public IMaterialization<TKey, TModel> Materialize(Tracker<TKey, TModel> tracker, TModel model)
    {
        return new Materialized<TKey, TModel>(Applicator, tracker, model);
    }
}

class Materialized<TKey, TModel> : IMaterialization<TKey, TModel>
    where TKey : struct
{
    IDomainEventApplicator Applicator;
    Tracker<TKey, TModel> Tracker;

    public Materialized(
        IDomainEventApplicator applicator,
        Tracker<TKey, TModel> tracker,
        TModel model)
    {
        Applicator = applicator ?? throw new ArgumentNullException(nameof(applicator));
        Tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        Model = model ?? throw new ArgumentNullException(nameof(model));
    }

    public TKey Key => Tracker;
    public TModel Model { get; }

    public void Emit<TEvent>(TEvent @event) where TEvent : IDomainEvent
    {
        Tracker.Add(@event);
        Applicator.Apply(Model, @event);
    }
}

interface IAggregate<TKey, TModel>
{
    TKey Key { get; }
    void Emit<TEvent>(TEvent @event) where TEvent : IDomainEvent;
    void Reset();
    Task<TModel> ToModelAsync(CancellationToken cancellationToken = default);
    Task SaveToAsync(IAggregateStore<TKey, TModel> storage, CancellationToken cancellationToken = default);
}

class Distribution
{
    public long SequenceId { get; set; }
    public Guid CommitId { get; set; }
}

interface IAggregateStore<TKey, TModel>
{
    Task<Guid> CommitAsync(TKey key, Guid? lastCommitId, IEnumerable<IDomainEvent> events, CancellationToken cancellationToken);
}

class TestStore<TKey, TModel> : IAggregateStore<TKey, TModel>
    where TKey : struct
{
    public Task<Guid> CommitAsync(TKey key, Guid? lastCommitId, IEnumerable<IDomainEvent> events, CancellationToken cancellationToken)
    {
        var commit = new Commit<TKey>
        {
            Id = Guid.NewGuid(),
            PreviousId = lastCommitId,
            Events = events.ToArray(),
            Timestamp = DateTimeOffset.Now,
            EntityId = key
        };
        return Task.FromResult(commit.Id);
    }
}

interface IDispatcher<TKey>
    where TKey : struct
{

}

class AggregatePublisher<TKey>
    where TKey : struct
{
    IQueryable<Commit<TKey>> Commits;
    IQueryable<Distribution> Queue;
    long Index = 0;
    Task<Commit<TKey>> GetNextCommitAsync(Guid? previousId, CancellationToken cancellationToken)
    {
        var records = Queue
            .Where(x => x.SequenceId >= Index)
            .Join(Commits, x => x.CommitId, x => x.Id, (x, Commit) => new { x.SequenceId, Commit })
            .OrderBy(x => x.SequenceId)
            .Take(100);

        foreach (var record in records)
        {
            Index = record.SequenceId;
        }

        return Task.FromResult(Commits.FirstOrDefault(x => x.PreviousId == previousId));
    }

    public AggregatePublisher(
        IDispatcher<TKey> dispatcher)
    {
    }

    public async Task ExecuteAsync(Guid? lastCommitId, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var commit = await GetNextCommitAsync(lastCommitId, cancellationToken)
                .ConfigureAwait(false);
            foreach (var @event in commit.Events)
            {
                
            }
        }
    }

    public async IAsyncEnumerable<Commit<TKey>> GetDomainEvents(Guid? lastCommitId, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            yield return await GetNextCommitAsync(lastCommitId, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}

class Publisher
{
    
}

class Tracker<TKey, TModel>
    where TKey : struct
{
    TKey Key;
    Guid? LastCommitId;
    LinkedList<IDomainEvent> Events = new LinkedList<IDomainEvent>();

    public Tracker(
        TKey key,
        Guid? lastCommitId)
    {
        Key = key;
        LastCommitId = lastCommitId;
    }

    public void Add(IDomainEvent @event)
    {
        Events.AddLast(@event);
    }

    public IEnumerable<IDomainEvent> Enumerate() => Events;
}

class Aggregator<TKey, TModel> : IAggregator<TKey, TModel>
    where TKey : struct
{
    Tracker<TKey, TModel> Tracker { get; }
    string Snapshot { get; }
    IModelSerializer Serializer { get; }
    TModel Model { get; }
    IEventApplicator Applicator;

    internal Aggregator(
        string snapshot)
    {
        Snapshot = snapshot;
    }

    public async Task EmitAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        await Applicator.ApplyAsync(Model, @event, cancellationToken)
            .ConfigureAwait(false);
        Tracker.Add(@event);
    }

    public async Task<TModel> ToModelAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var model = await Serializer
            .DeserializeAsync<TModel>(Snapshot, cancellationToken)
            .ConfigureAwait(false);
        foreach (var @event in Tracker.Enumerate())
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Applicator
                .ApplyAsync(model, @event, cancellationToken)
                .ConfigureAwait(false);
        }
        return model;
    }
}

class AggregatorFactory : IAggregationProvider
{
    IModelSerializer Serializer;
    public async Task<IAggregator<TKey, TModel>> Create<TKey, TModel>(TKey key, TModel model, CancellationToken cancellationToken) where TKey : struct
    {
        var serialized = await Serializer
            .SerializeAsync(model, cancellationToken)
            .ConfigureAwait(false);
        return new Aggregator<TKey, TModel>(serialized);
    }

    public Task<IAggregator<TKey, TModel>> GetAsync<TKey, TModel>(TKey key, IAggregationStorage<TKey, TModel> storage, CancellationToken cancellationToken) where TKey : struct
    {
        return storage.HydrateAsync(
            key,
            new Hydrator<TKey, TModel>(),
            cancellationToken);
    }
}

class Hydrator<TKey, TModel> : 
    IAggregationHydrator<TKey, TModel>
    where TKey : struct
{
    readonly TKey Key;

    public Hydrator(
        TKey key) 
    {
        Key = key;
    }

    public Task<IAggregator<TKey, TModel>> HydrateAsync(string snapshot, IEnumerable<IDomainEvent> events, CancellationToken cancellationToken)
    {
        var tracker = new Tracker<TKey>(Key);

        throw new NotImplementedException();
    }
}

class Aggregation<TKey, TModel> : IAggregate<TKey, TModel>
    where TKey : struct
{
    Tracker<TKey, TModel> Tracker;
    TModel Model;

    public TKey Key { get; }
    string Snapshot;
    Guid CommitId;
    readonly LinkedList<IDomainEvent> Events = new LinkedList<IDomainEvent>();
    readonly IDomainEventApplicator Applicator;
    readonly IModelSerializer Serializer;

    public Aggregation()
    {

    }

    public void Emit<TEvent>(TEvent @event)
        where TEvent : IDomainEvent
    {
        Tracker.Add(@event);

        Events.AddLast(@event);
    }

    public void Reset()
    {
        Events.Clear();
    }

    public async Task<TModel> ToModelAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var model = await Serializer
            .DeserializeAsync<TModel>(Snapshot)
            .ConfigureAwait(false);
        foreach (var @event in Events)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Applicator.Apply(model, @event);
        }
        return model;
    }

    public async Task SaveToAsync(IAggregateStore<TKey, TModel> storage, CancellationToken cancellationToken)
    {
        var model = await ToModelAsync(cancellationToken)
            .ConfigureAwait(false);
        var serialized = await Serializer
            .SerializeAsync(model, cancellationToken)
            .ConfigureAwait(false);
        CommitId = await storage.CommitAsync(Key, CommitId, Events, cancellationToken)
            .ConfigureAwait(false);
        Snapshot = serialized;
        Events.Clear();
    }
}

class VendingMachine
{
    public int Items { get; set; }
}

public class IntegrationEvent<TEvent>
    where TEvent : IDomainEvent
{
    public TEvent Event { get; }
    public DateTimeOffset Timestamp { get; }
}

class ItemsAddedEvent : IDomainEvent
{
    public int Count { get; set; }
}

class ItemsRemovedEvent : IDomainEvent
{
    public int Count { get; set; }
}

class AddItemsAggregateEventHandler : IDomainEventApplicator<VendingMachine, ItemsAddedEvent>
{
    public void Apply(VendingMachine model, ItemsAddedEvent @event)
    {
        if (model.Items + @event.Count > 5) throw new Exception("Too many items");
        model.Items += @event.Count;
    }
}

class RemoveItemsAggregateEventHandler : IDomainEventApplicator<VendingMachine, ItemsRemovedEvent>
{
    public void Apply(VendingMachine model, ItemsRemovedEvent @event)
    {
        if (model.Items - @event.Count < 0) throw new Exception("Cannot hold negative count of items");
        model.Items -= @event.Count;
    }
}

class VendingMachineInterface
{
    readonly IMaterialization<Guid, VendingMachine> Materialization;

    public VendingMachineInterface(
        IMaterialization<Guid, VendingMachine> materialization)
    {
        Materialization = materialization ?? throw new ArgumentNullException(nameof(materialization));
    }

    public void AddItems(int items)
    {
        Materialization.Emit(new ItemsAddedEvent { Count = items });
    }

    public void RemoveItems(int items)
    {
        Materialization.Emit(new ItemsRemovedEvent { Count = items });
    }
}

public interface IAggregateInterfaceFactory<T>
{

}

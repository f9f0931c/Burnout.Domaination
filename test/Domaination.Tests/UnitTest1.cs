using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Domaination.Tests
{
    public class UnitTest1
    {
        interface IFakeAggregate {
            void SetSomething();
        }

        class FakeAggregate : Aggregate<Guid>,
            IFakeAggregate {
            void Handle(FakeDomainEvent @event) 
            {
                // Do something
                Emit(@event);
            }

            public void SetSomething() 
            {
                Handle(new FakeDomainEvent());
            }
        }

        class FakeDomainEvent : IDomainEvent {

        }

        class FakeAggregateStore : IAggregateStore
        {
            public Task CommitAsync(IEnumerable<StagedEvent> events, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task Test1()
        {
            var store = new FakeAggregateStore();
            var aggregate = new FakeAggregate();
            aggregate.SetSomething();
            await aggregate.CommitAsync(store, CancellationToken.None);
        }
    }
}

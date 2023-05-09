using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eventuous.TestHelpers;
public class MockAggregateStore : IAggregateStore
{
    readonly object _aggregateRoot;

    public MockAggregateStore(object aggregateRoot) => _aggregateRoot = aggregateRoot;

    public Task<T> Load<T>(StreamName streamName, CancellationToken cancellationToken) where T : Aggregate
        => Task.FromResult((T)_aggregateRoot);

    public Task<T> LoadOrNew<T>(StreamName streamName, CancellationToken cancellationToken) where T : Aggregate
        => _aggregateRoot is null ? Task.FromResult((T)Activator.CreateInstance(typeof(T))!) : Task.FromResult((T)_aggregateRoot);

    public Task<AppendEventsResult> Store<T>(StreamName streamName, T aggregate, CancellationToken cancellationToken)
        where T : Aggregate
    {
        var changedVersion = aggregate.CurrentVersion + aggregate.Changes.Count;
        var nextVersion = changedVersion + 1;
        var globalPosition = (ulong)changedVersion + 1000;

        return Task.FromResult(new AppendEventsResult(globalPosition, nextVersion));
    }
}

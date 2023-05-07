using System.Collections.Concurrent;

using Eventuous.Subscriptions.Checkpoints;

using Microsoft.Extensions.Logging;

using Raven.Client.Documents;
using Raven.Client.Documents.Commands;
using Raven.Client.Documents.Session;

namespace BuildingBlocks.Projections.RavenDB;
public sealed class RavenCheckpointStore : ICheckpointStore
{
    static readonly string CheckpointTypeName = typeof(Checkpoint).Name;

    readonly ILogger<RavenCheckpointStore> _logger;
    readonly IDocumentStore _documentStore;
    readonly DocumentInfo _documentInfo;
    readonly ConcurrentDictionary<string, int> _counters;
    readonly int _batchSize;

    public RavenCheckpointStore(ILogger<RavenCheckpointStore> logger, IDocumentStore documentStore, int batchSize)
    {
        _logger = logger;
        _documentStore = documentStore;
        _documentInfo = new DocumentInfo { Collection = "Checkpoints" };
        _counters = new();
        _batchSize = batchSize;
    }

    public async ValueTask<Checkpoint> GetLastCheckpoint(string checkpointId, CancellationToken cancellationToken = default)
    {
        using var session = _documentStore.OpenAsyncSession();

        var checkpoint = await session.LoadAsync<Checkpoint?>($"Checkpoints/{checkpointId}", cancellationToken).ConfigureAwait(false);

        if (checkpoint is null)
        {
            checkpoint = new Checkpoint(checkpointId, null);

            await session.StoreAsync(checkpoint, $"Checkpoints/{checkpoint.Value.Id}", cancellationToken).ConfigureAwait(false);
            await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        _counters[checkpointId] = 0;

        _logger.LogInformation("[GetLastCheckpoint] Id: {CheckpointId}, Position: {Position}", checkpointId, checkpoint.Value.Position);

        return checkpoint.Value;
    }

    public async ValueTask<Checkpoint> StoreCheckpoint(Checkpoint checkpoint, bool force, CancellationToken cancellationToken = default)
    {
        int count = ++_counters[checkpoint.Id];

        if (!force && count < _batchSize) return checkpoint;

        using var session = _documentStore.OpenAsyncSession();

        var blittableCheckpoint = session.Advanced.JsonConverter.ToBlittable(checkpoint, _documentInfo);
        var replaceCmd = new PutDocumentCommand($"Checkpoints/{checkpoint.Id}", null, blittableCheckpoint);

        await session.Advanced.RequestExecutor.ExecuteAsync(replaceCmd, session.Advanced.Context).ConfigureAwait(false);

        _logger.LogInformation("[StoreCheckpoint] Id: {CheckpointId}, Position: {Position}", checkpoint.Id, checkpoint.Position);

        return checkpoint;
    }
}

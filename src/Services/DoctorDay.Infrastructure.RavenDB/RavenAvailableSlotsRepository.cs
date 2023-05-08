using DoctorDay.Application.Queries;
using DoctorDay.Domain;

using Eventuous.Subscriptions;

using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;

namespace DoctorDay.Infrastructure.RavenDB;

public sealed class RavenAvailableSlotsRepository : IAvailableSlotsRepository
{
    readonly IDocumentStore _documentStore;
    readonly string _prefix;

    public RavenAvailableSlotsRepository(IDocumentStore documentStore)
    {
        _documentStore = documentStore;

        var pluralisedName = DocumentConventions.DefaultGetCollectionName(typeof(ReadModels.AvailableSlot));
        _prefix = _documentStore.Conventions.TransformTypeCollectionNameToDocumentIdPrefix(pluralisedName);
    }

    public async Task<IEnumerable<ReadModels.AvailableSlot>> GetAvailableSlotsOn(DateTimeOffset date, CancellationToken cancellationToken)
    {
        if (date == default)
        {
            throw new ArgumentException(nameof(date));
        }
            
        using var session = _documentStore.OpenAsyncSession();

        //This will pull from local DocumentStore cache unless there are updates
        //Because of aggressive caching, could be stale for a second or two after
        //an update but that should be fine.
        var archivableDays = await session
            .Query<ReadModels.AvailableSlot>()
            .Where(x => x.Date == date.Date.ToString("yyyy-MM-dd"))
            .Where(x => x.IsBooked == false)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return archivableDays.AsEnumerable();
    }

    public async Task<EventHandlingStatus> AddSlot(ReadModels.AvailableSlot slot, CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();

        var slotFullId = GetFullId(slot.Id);

        bool slotDoesNotExist = !await session.Advanced.ExistsAsync(slotFullId, cancellationToken).ConfigureAwait(false);

        if (slotDoesNotExist)
        {
            await session.StoreAsync(slot, slotFullId, cancellationToken).ConfigureAwait(false);
            await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return EventHandlingStatus.Success;
    }

    public async Task<EventHandlingStatus> HideSlot(Guid slotId, ulong position, CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();

        var slot = await session
            .LoadAsync<ReadModels.AvailableSlot>(GetFullId(slotId), cancellationToken)
            .ConfigureAwait(false);

        bool eventHasNotBeenHandled = position > slot.Position;

        if (eventHasNotBeenHandled)
        {
            slot.IsBooked = true;
            slot.Position = position;

            await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return EventHandlingStatus.Success;
    }

    public async Task<EventHandlingStatus> ShowSlot(Guid slotId, ulong position, CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();

        var slot = await session
            .LoadAsync<ReadModels.AvailableSlot>(GetFullId(slotId), cancellationToken)
            .ConfigureAwait(false);

        bool eventHasNotBeenHandled = position > slot.Position;

        if (eventHasNotBeenHandled)
        {
            slot.IsBooked = false;
            slot.Position = position;

            await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return EventHandlingStatus.Success;
    }

    public async Task<EventHandlingStatus> DeleteSlot(Guid slotId, ulong position, CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();

        var slot = await session
            .LoadAsync<ReadModels.AvailableSlot>(GetFullId(slotId), cancellationToken)
            .ConfigureAwait(false);

        bool eventHasNotBeenHandled = slot is not null && position > slot.Position;

        if (eventHasNotBeenHandled)
        {
            session.Delete(slot);

            await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return EventHandlingStatus.Success;
    }

    string GetFullId(Guid shortId) => $"{_prefix}/{shortId}";
    string GetFullId(string shortId) => $"{_prefix}/{shortId}";
}

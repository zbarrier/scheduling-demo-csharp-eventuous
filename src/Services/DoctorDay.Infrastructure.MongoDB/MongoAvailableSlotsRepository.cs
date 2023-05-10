using DoctorDay.Application.Queries;
using DoctorDay.Domain;

using Eventuous.Subscriptions;

using MongoDB.Driver;

namespace DoctorDay.Infrastructure.MongoDB;

public sealed class MongoAvailableSlotsRepository : IAvailableSlotsRepository
{
    readonly IMongoCollection<ReadModels.AvailableSlot> _collection;

    public MongoAvailableSlotsRepository(IMongoClient client) 
        => _collection = client.GetDatabase("projections").GetCollection<ReadModels.AvailableSlot>("available_slots");

    public async Task<IEnumerable<ReadModels.AvailableSlot>> GetAvailableSlotsOn(DateTimeOffset date, CancellationToken cancellationToken)
    {
        var filter = Builders<ReadModels.AvailableSlot>.Filter
            .Where(x => x.Date == date.Date.ToString("yyyy-MM-dd") && x.IsBooked == false);

        return (await _collection.FindAsync(filter).ConfigureAwait(false)).ToList();
    }

    public async Task<EventHandlingStatus> AddSlot(ReadModels.AvailableSlot availableSlot, CancellationToken cancellationToken)
    {
        await _collection.InsertOneAsync(availableSlot).ConfigureAwait(false);
        return EventHandlingStatus.Success;
    }

    public async Task<EventHandlingStatus> HideSlot(Guid slotId, ulong position, CancellationToken cancellationToken)
    {
        var filter = Builders<ReadModels.AvailableSlot>.Filter.Eq(x => x.Id, slotId.ToString());
        var update = Builders<ReadModels.AvailableSlot>.Update.Set(x => x.IsBooked, true);
        await _collection.UpdateOneAsync(filter, update).ConfigureAwait(false);
        return EventHandlingStatus.Success;
    }

    public async Task<EventHandlingStatus> ShowSlot(Guid slotId, ulong position, CancellationToken cancellationToken)
    {
        var filter = Builders<ReadModels.AvailableSlot>.Filter.Eq(x => x.Id, slotId.ToString());
        var update = Builders<ReadModels.AvailableSlot>.Update.Set(x => x.IsBooked, false);
        await _collection.UpdateOneAsync(filter, update).ConfigureAwait(false);
        return EventHandlingStatus.Success;
    }

    public async Task<EventHandlingStatus> DeleteSlot(Guid slotId, ulong position, CancellationToken cancellationToken)
    {
        var filter = Builders<ReadModels.AvailableSlot>.Filter.Eq(x => x.Id, slotId.ToString());
        await _collection.FindOneAndDeleteAsync(filter).ConfigureAwait(false);
        return EventHandlingStatus.Success;
    }
}

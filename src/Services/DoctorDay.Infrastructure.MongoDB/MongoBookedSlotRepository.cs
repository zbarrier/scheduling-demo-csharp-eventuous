using DoctorDay.Application.Queries;
using DoctorDay.Domain;

using MongoDB.Driver;

namespace DoctorDay.Infrastructure.MongoDB;

public sealed class MongoBookedSlotRepository : IBookedSlotsRepository
{
    readonly IMongoCollection<ReadModels.BookedSlot> _collection;

    public MongoBookedSlotRepository(IMongoClient client)
        => _collection = client.GetDatabase("projections").GetCollection<ReadModels.BookedSlot>("booked_slot");

    public async Task<int> CountByPatientAndYearAndMonth(string patientId, int year, int month, CancellationToken cancellationToken)
    {
        var result = await _collection.FindAsync(x => x.PatientId == patientId && x.Month == month, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return result.ToList(cancellationToken).Count;
    }

    public Task AddSlot(ReadModels.BookedSlot slot, CancellationToken cancellationToken)
        => _collection.InsertOneAsync(slot, cancellationToken: cancellationToken);

    public async Task<ReadModels.BookedSlot> MarkSlotAsBooked(Guid slotId, string patientId, ulong position, CancellationToken cancellationToken)
    {
        var filter = Builders<ReadModels.BookedSlot>.Filter.Eq(x => x.Id, slotId.ToString());
        var update = Builders<ReadModels.BookedSlot>.Update
            .Set(x => x.IsBooked, true)
            .Set(x => x.PatientId, patientId);

        await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var bookedSlot = await GetSlot(slotId, cancellationToken)
            .ConfigureAwait(false);

        return bookedSlot;
    }

    public Task MarkSlotAsAvailable(Guid slotId, ulong position, CancellationToken cancellationToken)
    {
        var filter = Builders<ReadModels.BookedSlot>.Filter.Eq(x => x.Id, slotId.ToString());
        var update = Builders<ReadModels.BookedSlot>.Update
            .Set(x => x.IsBooked, false)
            .Set(x => x.PatientId, string.Empty);
        return _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    public Task DeleteSlot(Guid slotId, ulong position, CancellationToken cancellationToken)
        => _collection.DeleteOneAsync<ReadModels.BookedSlot>(x => x.Id == slotId.ToString(), cancellationToken);

    async Task<ReadModels.BookedSlot> GetSlot(Guid slotId, CancellationToken cancellationToken)
        => (await _collection.FindAsync(x => x.Id == slotId.ToString()).ConfigureAwait(false)).FirstOrDefault();
}

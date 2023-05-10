using DoctorDay.Application.Queries;
using DoctorDay.Domain;

using MongoDB.Driver;

namespace DoctorDay.Infrastructure.MongoDB;

public sealed class MongoArchivableDaysRepository : IArchivableDaysRepository 
{
    readonly IMongoCollection<ReadModels.ArchivableDay> _collection;

    public MongoArchivableDaysRepository(IMongoClient client) 
        => _collection = client.GetDatabase("projections").GetCollection<ReadModels.ArchivableDay>("archivable_day");

    public Task Add(ReadModels.ArchivableDay archivableDay, CancellationToken cancellationToken) 
        => _collection.InsertOneAsync((ReadModels.ArchivableDay)archivableDay);

    public async Task<IEnumerable<ReadModels.ArchivableDay>> FindAll(DateTimeOffset dateThreshold, CancellationToken cancellationToken) 
    {
        var filter = Builders<ReadModels.ArchivableDay>.Filter.Where(x => x.Date <= dateThreshold);
        var availableSlots = await _collection.FindAsync(filter).ConfigureAwait(false);
        return availableSlots.ToList();
    }
}

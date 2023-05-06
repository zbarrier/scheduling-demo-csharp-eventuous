using DoctorDay.Application.Queries;
using DoctorDay.Domain;

using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace DoctorDay.Infrastructure.RavenDB;

public sealed class RavenArchivableDaysRepository : IArchivableDaysRepository
{
    readonly IDocumentStore _documentStore;
    readonly SessionOptions _noTrackingAndNoCachingOptions = new SessionOptions { NoTracking = true, NoCaching = true };

    public RavenArchivableDaysRepository(IDocumentStore documentStore) => _documentStore = documentStore;

    public async Task<IEnumerable<ReadModels.ArchivableDay>> FindAll(DateTimeOffset dateThreshold, CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession(_noTrackingAndNoCachingOptions);

        var archivableDays = await session
            .Query<ReadModels.ArchivableDay>()
            .Customize(x => x.WaitForNonStaleResults(TimeSpan.FromSeconds(5)))
            .Where(x => x.Date <= dateThreshold)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return archivableDays.AsEnumerable();
    }

    public async Task Add(ReadModels.ArchivableDay archivableDay, CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();

        await session.StoreAsync(archivableDay, $"ArchivableDays/{archivableDay.Id}", cancellationToken).ConfigureAwait(false);
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

using DoctorDay.Domain;

namespace DoctorDay.Application.Queries;
public interface IArchivableDaysRepository
{
    Task<IEnumerable<ReadModels.ArchivableDay>> FindAll(DateTimeOffset dateThreshold, CancellationToken cancellationToken);
    Task Add(ReadModels.ArchivableDay archivableDay, CancellationToken cancellationToken);
}

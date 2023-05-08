using DoctorDay.Domain;

using Eventuous.Subscriptions;

namespace DoctorDay.Application.Queries;
public interface IBookedSlotsRepository
{
    Task<int> CountByPatientAndYearAndMonth(string patientId, int year, int month, CancellationToken cancellationToken);

    Task AddSlot(ReadModels.BookedSlot slot, CancellationToken cancellationToken);
    Task MarkSlotAsAvailable(Guid slotId, ulong position, CancellationToken cancellationToken);
    Task<ReadModels.BookedSlot> MarkSlotAsBooked(Guid slotId, string patientId, ulong position, CancellationToken cancellationToken);
    Task DeleteSlot(Guid slotId, ulong position, CancellationToken cancellationToken);
}

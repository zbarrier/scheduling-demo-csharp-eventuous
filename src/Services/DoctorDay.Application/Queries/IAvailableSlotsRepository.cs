using DoctorDay.Domain;

using Eventuous.Subscriptions;

namespace DoctorDay.Application.Queries;
public interface IAvailableSlotsRepository
{
    Task<IEnumerable<ReadModels.AvailableSlot>> GetAvailableSlotsOn(DateTimeOffset date, CancellationToken cancellationToken);

    Task<EventHandlingStatus> AddSlot(ReadModels.AvailableSlot availableSlot, CancellationToken cancellationToken);
    Task<EventHandlingStatus> HideSlot(Guid slotId, ulong position, CancellationToken cancellationToken);
    Task<EventHandlingStatus> ShowSlot(Guid slotId, ulong position, CancellationToken cancellationToken);
    Task<EventHandlingStatus> DeleteSlot(Guid slotId, ulong position, CancellationToken cancellationToken);
}

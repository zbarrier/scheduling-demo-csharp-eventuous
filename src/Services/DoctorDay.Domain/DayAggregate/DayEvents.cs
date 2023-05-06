using Eventuous;

namespace DoctorDay.Domain.DayAggregate;
public static class DayEvents
{
    [EventType(nameof(CalendarDayStarted_V1))]
    public sealed record CalendarDayStarted_V1(DateTimeOffset Date);

    [EventType(nameof(DayScheduled_V1))]
    public sealed record DayScheduled_V1(Guid DoctorId, DateTimeOffset Date);

    [EventType(nameof(SlotScheduled_V1))]
    public sealed record SlotScheduled_V1(Guid SlotId, DateTimeOffset SlotStartTime, TimeSpan SlotDuration);

    [EventType(nameof(SlotBooked_V1))]
    public sealed record SlotBooked_V1(Guid SlotId, string PatientId);

    [EventType(nameof(SlotBookingCancelled_V1))]
    public sealed record SlotBookingCancelled_V1(Guid SlotId, string Reason);

    [EventType(nameof(SlotScheduleCancelled_V1))]
    public sealed record SlotScheduleCancelled_V1(Guid SlotId);

    [EventType(nameof(DayScheduleCancelled_V1))]
    public sealed record DayScheduleCancelled_V1();

    [EventType(nameof(DayScheduleArchived_V1))]
    public sealed record DayScheduleArchived_V1();

    public static void MapBookingEvents()
        => TypeMap.RegisterKnownEventTypes();
}

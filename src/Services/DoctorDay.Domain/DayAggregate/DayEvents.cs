using Eventuous;

namespace DoctorDay.Domain.DayAggregate;
public static class DayEvents
{
    public static class V1
    {
        [EventType("V1.CalendarDayStarted")]
        public sealed record CalendarDayStarted(DateTimeOffset Date);

        [EventType("V1.DayScheduled")]
        public sealed record DayScheduled(Guid DoctorId, DateTimeOffset Date);

        [EventType("V1.SlotScheduled")]
        public sealed record SlotScheduled(Guid SlotId, DateTimeOffset SlotStartTime, TimeSpan SlotDuration);

        [EventType("V1.SlotBooked")]
        public sealed record SlotBooked(Guid SlotId, string PatientId);

        [EventType("V1.SlotBookingCancelled")]
        public sealed record SlotBookingCancelled(Guid SlotId, string Reason);

        [EventType("V1.SlotScheduleCancelled")]
        public sealed record SlotScheduleCancelled(Guid SlotId);

        [EventType("V1.DayScheduleCancelled")]
        public sealed record DayScheduleCancelled();

        [EventType("V1.DayScheduleArchived")]
        public sealed record DayScheduleArchived();
    }
}

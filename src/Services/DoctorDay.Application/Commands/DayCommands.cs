using DoctorDay.Domain;

namespace DoctorDay.Application.Commands;
public static class DayCommands
{
    public sealed record StartCalendarDay(DateTimeOffset Date);
    public sealed record ScheduleDay(Guid DoctorId, DateTimeOffset Date, IEnumerable<SlotToSchedule> SlotsToSchedule);
    public sealed record ScheduleSlot(string DayId, DateTimeOffset StartTime, TimeSpan Duration);
    public sealed record BookSlot(string DayId, Guid SlotId, string PatientId);
    public sealed record CancelSlotBooking(string DayId, Guid SlotId, string Reason);
    public sealed record CancelDaySchedule(string DayId);
    public sealed record ArchiveDaySchedule(string DayId);
}

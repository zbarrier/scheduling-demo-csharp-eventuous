using DoctorDay.Domain;

namespace DoctorDay.Application.Commands;
public static class DayCommands
{
    public sealed record StartCalendarDay(DateTime Date);
    public sealed record ScheduleDay(Guid DoctorId, DateTime Date, IEnumerable<SlotToSchedule> SlotsToSchedule);
    public sealed record ScheduleSlot(string DayId, DateTime StartTime, TimeSpan Duration);
    public sealed record BookSlot(string DayId, Guid SlotId, string PatientId);
    public sealed record CancelSlotBooking(string DayId, Guid SlotId, string Reason);
    public sealed record CancelDaySchedule(string DayId);
    public sealed record ArchiveDaySchedule(string DayId);
}

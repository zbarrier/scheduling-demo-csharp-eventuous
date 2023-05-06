using DoctorDay.Domain;
using DoctorDay.Domain.DayAggregate;

using Eventuous;

namespace DoctorDay.Application.Commands;
public sealed class DayService : CommandService<Day, DayState, DayId>
{
    public DayService(IAggregateStore store, StreamNameMap? streamNameMap = null)
        : base(store, streamNameMap:  streamNameMap)
    {
        OnNew<DayCommands.ScheduleDay>(
            cmd => DayId.Create(new DoctorId(cmd.DoctorId), cmd.Date),
            (day, cmd) =>
            {
                var doctorId = new DoctorId(cmd.DoctorId);

                day.Schedule(doctorId, cmd.Date, cmd.SlotsToSchedule, Guid.NewGuid);
            });

        OnExisting<DayCommands.ScheduleSlot>(
            cmd => new DayId(cmd.DayId),
            (day, cmd) => day.ScheduleSlot(cmd.StartTime, cmd.Duration, Guid.NewGuid));

        OnExisting<DayCommands.BookSlot>(
            cmd => new DayId(cmd.DayId),
            (day, cmd) => 
            {
                var slotId = new SlotId(cmd.SlotId);
                var patientId = new PatientId(cmd.PatientId);

                day.BookSlot(slotId, patientId);
            });

        OnExisting<DayCommands.CancelSlotBooking>(
            cmd => new DayId(cmd.DayId),
            (day, cmd) =>
            {
                var slotId = new SlotId(cmd.SlotId);

                day.CancelSlotBooking(slotId, cmd.Reason);
            });

        OnExisting<DayCommands.CancelDaySchedule>(
            cmd => new DayId(cmd.DayId),
            (day, cmd) => day.Cancel());

        OnExisting<DayCommands.ArchiveDaySchedule>(
            cmd => new DayId(cmd.DayId),
            (day, cmd) => day.Archive());
    }
}

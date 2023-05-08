using Eventuous;

using static DoctorDay.Domain.DayAggregate.DayEvents;

namespace DoctorDay.Domain.DayAggregate;
public sealed class Day : Aggregate<DayState>
{
    internal const int MaximumScheduledHoursPerDay = 6;
    internal const int RequiredSlotDurationInMinutes = 10;
    internal const int MaximumScheduledSlotsPerDay = MaximumScheduledHoursPerDay * 60 / RequiredSlotDurationInMinutes;

    public void Schedule(DoctorId doctorId, DateTimeOffset date, IEnumerable<SlotToSchedule> slotsToSchedule, Func<Guid> idGenerator)
    {
        EnsureDayNotScheduled();
        EnsureDayNotArchived();
        EnsureDayNotCancelled();

        _ = Apply(new V1.DayScheduled(doctorId, date));

        foreach (var slotToSchedule in slotsToSchedule)
        {
            _ = Apply(new V1.SlotScheduled(idGenerator(), slotToSchedule.StartTime, slotToSchedule.Duration));
        }
    }

    public void ScheduleSlot(DateTimeOffset startTime, TimeSpan duration, Func<Guid> idGenerator)
    {
        EnsureDayScheduled();
        EnsureDayNotArchived();
        EnsureDayNotCancelled();

        EnsureSlotIsSameAsScheduledDay(startTime);
        EnsureSlotDurationIsValid(duration);
        EnsureDayNotFull();
        EnsureSlotDoesNotOverlapWithExistingSlots(startTime, duration);

        _ = Apply(new V1.SlotScheduled(idGenerator(), startTime, duration));
    }

    public void BookSlot(SlotId slotId, PatientId patientId)
    {
        EnsureDayScheduled();
        EnsureDayNotArchived();
        EnsureDayNotCancelled();

        EnsureSlotScheduled(slotId);
        EnsureSlotNotBooked(slotId);

        _ = Apply(new V1.SlotBooked(slotId, patientId));
    }

    public void CancelSlotBooking(SlotId slotId, string reason)
    {
        EnsureDayScheduled();
        EnsureDayNotArchived();
        EnsureDayNotCancelled();

        EnsureSlotScheduled(slotId);
        EnsureSlotBooked(slotId);

        _ = Apply(new V1.SlotBookingCancelled(slotId, reason));
    }

    public void Cancel()
    {
        EnsureDayScheduled();
        EnsureDayNotArchived();
        EnsureDayNotCancelled();

        foreach (var bookedSlot in State.BookedSlots)
        {
            _ = Apply(new V1.SlotBookingCancelled(bookedSlot.Id, "Day cancelled."));
        }
            
        foreach (var slot in State.AllSlots)
        {
            _ = Apply(new V1.SlotScheduleCancelled(slot.Id));
        }
           
        _ = Apply(new V1.DayScheduleCancelled());
    }

    public void Archive()
    {
        EnsureDayScheduled();
        EnsureDayNotArchived();

        _ = Apply(new V1.DayScheduleArchived());
    }


    void EnsureDayNotScheduled() { if (CurrentVersion >= 0) throw DayExceptions.NewDayAlreadyScheduled(); }
    void EnsureDayScheduled() { if (CurrentVersion < 0) throw DayExceptions.NewDayNotScheduled(); }
    void EnsureDayNotArchived() { if (State.Archived) throw DayExceptions.NewDayScheduleAlreadyArchived(); }
    void EnsureDayNotCancelled() { if (State.Cancelled) throw DayExceptions.NewDayScheduleAlreadyCancelled(); }
    void EnsureDayNotFull() { if (State.Full) throw DayExceptions.NewDayAlreadyFull(); }

    void EnsureSlotIsSameAsScheduledDay(DateTimeOffset startTime) { if (!State.SameDateAsScheduledDay(startTime)) throw DayExceptions.NewSlotIsForWrongDay(); }
    void EnsureSlotDurationIsValid(TimeSpan duration) { if (TimeSpan.FromMinutes(RequiredSlotDurationInMinutes) != duration) throw DayExceptions.NewSlotDurationInvalid(); }
    void EnsureSlotDoesNotOverlapWithExistingSlots(DateTimeOffset startTime, TimeSpan duration) { if (State.OverlapsWithScheduledSlots(startTime, duration)) throw DayExceptions.NewSlotOverlaps(); }
    void EnsureSlotScheduled(SlotId slotId) { if (State.SlotNotScheduled(slotId)) throw DayExceptions.NewSlotNotScheduled(); }
    void EnsureSlotNotBooked(SlotId slotId) { if (State.SlotBooked(slotId)) throw DayExceptions.NewSlotAlreadyBooked(); }
    void EnsureSlotBooked(SlotId slotId) { if (State.SlotNotBooked(slotId)) throw DayExceptions.NewSlotNotBooked(); }
}
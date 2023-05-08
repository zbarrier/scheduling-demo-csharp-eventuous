using System.Collections.Immutable;

using Eventuous;

using static DoctorDay.Domain.DayAggregate.DayEvents;

namespace DoctorDay.Domain.DayAggregate;
public sealed record DayState : State<DayState>
{
    public DayState()
    {
        On<V1.DayScheduled>(Handle);
        On<V1.SlotScheduled>(Handle);
        On<V1.SlotBooked>(Handle);
        On<V1.SlotBookingCancelled>(Handle);
        On<V1.SlotScheduleCancelled>(Handle);
        On<V1.DayScheduleCancelled>(Handle);
        On<V1.DayScheduleArchived>(Handle);
    }

    internal DateTimeOffset Date { get; init; }

    internal bool Archived { get; init; }
    internal bool NotArchived => !Archived;

    internal bool Cancelled { get; init; }
    internal bool NotCancelled => !Cancelled;

    internal ImmutableList<Slot> Slots { get; init; } = ImmutableList<Slot>.Empty;

    internal IEnumerable<Slot> AllSlots => Slots;
    internal IEnumerable<Slot> BookedSlots => Slots.Where(slot => slot.Booked);

    internal bool Full => Slots.Count == Day.MaximumScheduledSlotsPerDay;
    internal bool NotFull => Slots.Count < Day.MaximumScheduledSlotsPerDay;

    internal bool SameDateAsScheduledDay(DateTimeOffset startTime) => Date.Date == startTime.Date;
    internal bool OverlapsWithScheduledSlots(DateTimeOffset startTime, TimeSpan duration) => Slots.Any(slot => slot.OverlapsWith(startTime, duration));
    internal bool SlotScheduled(SlotId slotId) => Slots.Any(slot => slot.Id == slotId);
    internal bool SlotNotScheduled(SlotId slotId) => !SlotScheduled(slotId);
    internal bool SlotBooked(SlotId slotId) => Slots.FirstOrDefault(slot => slot.Id == slotId)?.Booked ?? false;
    internal bool SlotNotBooked(SlotId slotId) => !SlotBooked(slotId);

    static DayState Handle(DayState state, V1.DayScheduled e)
        => state with
        {
            Date = e.Date
        };
    static DayState Handle(DayState state, V1.SlotScheduled e)
        => state with
        {
            Slots = state.Slots.Add(new Slot(e.SlotId, e.SlotStartTime, e.SlotDuration))
        };
    static DayState Handle(DayState state, V1.SlotBooked e)
        => state with
        {
            Slots = state.Slots.Replace(
                state.Slots.Single(s => s.Id == e.SlotId),
                state.Slots.Single(s => s.Id == e.SlotId) with { Booked = true })
        };
    static DayState Handle(DayState state, V1.SlotBookingCancelled e)
        => state with
        {
            Slots = state.Slots.Replace(
                state.Slots.Single(s => s.Id == e.SlotId),
                state.Slots.Single(s => s.Id == e.SlotId) with { Booked = false })
        };
    static DayState Handle(DayState state, V1.SlotScheduleCancelled e)
        => state with
        {
            Slots = state.Slots.Remove(state.Slots.Single(s => s.Id == e.SlotId))
        };
    static DayState Handle(DayState state, V1.DayScheduleCancelled e)
        => state with
        {
            Cancelled = true
        };
    static DayState Handle(DayState state, V1.DayScheduleArchived e)
        => state with
        {
            Archived = true
        };
}

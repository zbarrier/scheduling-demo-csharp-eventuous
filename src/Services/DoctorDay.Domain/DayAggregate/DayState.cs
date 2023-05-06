using System.Collections.Immutable;
using Eventuous;

using static System.Reflection.Metadata.BlobBuilder;

namespace DoctorDay.Domain.DayAggregate;
public sealed record DayState : State<DayState>
{
    public DayState()
    {
        On<DayEvents.DayScheduled_V1>((state, evt) => state with 
        {
            Date = evt.Date
        });

        On<DayEvents.SlotScheduled_V1>((state, evt) => state with
        {
            Slots = Slots.Add(new Slot(evt.SlotId, evt.SlotStartTime, evt.SlotDuration))
        });

        On<DayEvents.SlotBooked_V1>((state, evt) => state with
        {
            Slots = Slots.Replace(
                Slots.Single(s => s.Id == evt.SlotId),
                Slots.Single(s => s.Id == evt.SlotId) with { Booked = true })
        });

        On<DayEvents.SlotBookingCancelled_V1>((state, evt) => state with
        {
            Slots = Slots.Replace(
                Slots.Single(s => s.Id == evt.SlotId),
                Slots.Single(s => s.Id == evt.SlotId) with { Booked = false })
        });

        On<DayEvents.SlotScheduleCancelled_V1>((state, evt) => state with
        {
            Slots = Slots.Remove(Slots.Single(s => s.Id == evt.SlotId))
        });

        On<DayEvents.DayScheduleCancelled_V1>((state, evt) => state with
        {
            Cancelled = true
        });

        On<DayEvents.DayScheduleArchived_V1>((state, evt) => state with
        {
            Archived = true
        });
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
}

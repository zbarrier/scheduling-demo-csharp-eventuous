using Eventuous.Diagnostics.Metrics;

namespace DoctorDay.Domain.DayAggregate;
internal sealed record Slot(Guid Id, DateTimeOffset StartTime, TimeSpan Duration)
{
    internal bool Booked { get; set; } = false;

    internal bool OverlapsWith(DateTimeOffset startTime, TimeSpan duration)
    {
        DateTimeOffset thisStart = StartTime;
        DateTimeOffset thisEnd = thisStart.Add(Duration);

        DateTimeOffset proposedStart = startTime;
        DateTimeOffset proposedEnd = proposedStart.Add(duration);

        return thisStart < proposedEnd && thisEnd > proposedStart;
    }

    public bool Equals(Slot? other) => Id == other?.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

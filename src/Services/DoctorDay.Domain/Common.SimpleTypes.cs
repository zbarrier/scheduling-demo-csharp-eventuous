using Eventuous;
using Eventuous.Diagnostics.Metrics;

namespace DoctorDay.Domain;

public record DayId(string Value) : AggregateId(Value)
{
    public static DayId Create(DoctorId doctorId, DateTimeOffset date) => new DayId($"{doctorId}_{date:yyyy-MM-dd}");
}

public sealed record SlotId(Guid Value)
{
    public static SlotId Create(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentNullException(nameof(value), "SlotId cannot be null or empty.");

        return new SlotId(value);
    }

    public static SlotId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(value), "SlotId cannot be null or empty.");
        if (!Guid.TryParse(value, out var slotId)) throw new ArgumentException("SlotId must be a valid GUID.", nameof(value));
        if (slotId == Guid.Empty) throw new ArgumentNullException(nameof(value), "SlotId cannot be null or empty.");

        return new SlotId(slotId);
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(SlotId id) => id.Value;
    public static implicit operator string(SlotId id) => id.ToString();
}

public sealed record DoctorId(Guid Value)
{
    public static DoctorId Create(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentNullException(nameof(value), "DoctorId cannot be null or empty.");

        return new DoctorId(value);
    }

    public static DoctorId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(value), "DoctorId cannot be null or empty.");
        if (!Guid.TryParse(value, out var doctorId)) throw new ArgumentException("DoctorId must be a valid GUID.", nameof(value));
        if (doctorId == Guid.Empty) throw new ArgumentNullException(nameof(value), "DoctorId cannot be null or empty.");

        return new DoctorId(doctorId);
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(DoctorId id) => id.Value;
    public static implicit operator string(DoctorId id) => id.ToString();
}

public sealed record PatientId(string Value)
{
    public static PatientId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(value), "PatientId cannot be null or empty.");

        return new PatientId(value);
    }

    public static implicit operator string(PatientId self) => self.Value;
}

public sealed record SlotToSchedule(DateTimeOffset StartTime, TimeSpan Duration);
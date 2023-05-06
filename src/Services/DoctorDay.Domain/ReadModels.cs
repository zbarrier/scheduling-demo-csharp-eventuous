namespace DoctorDay.Domain;
public static class ReadModels
{
    public sealed record ArchivableDay(string Id, DateTimeOffset Date)
    {
        public ulong Position { get; set; }
    }

    public sealed record AvailableSlot(string Id, string DayId, string Date, string StartTime, TimeSpan Duration)
    {
        public bool IsBooked { get; set; } = false;
        public ulong Position { get; set; }
    }

    public sealed record BookedSlot(string Id, string DayId, int Year, int Month)
    {
        public bool IsBooked { get; set; }
        public string? PatientId { get; set; }
        public ulong Position { get; set; }
    }
}

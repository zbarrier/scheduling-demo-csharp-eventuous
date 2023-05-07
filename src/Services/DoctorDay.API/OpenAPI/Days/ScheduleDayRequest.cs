using System;
using System.Collections.Generic;
using System.Linq;

using DoctorDay.Domain;

using static DoctorDay.Application.Commands.DayCommands;

namespace DoctorDay.API.OpenAPI.Days
{
    public sealed class ScheduleDayRequest
    {
        public Guid DoctorId { get; set; }
        public DateTimeOffset? Date { get; set; }
        public List<SlotRequest> Slots { get; set; }

        public ScheduleDay ToCommand()
            => new(
                DoctorId,
                Date ?? default,
                Slots.Select(s => new SlotToSchedule(Date.Value.Add(TimeSpan.Parse(s.StartTime)), s.Duration.Value)).ToList()
            );
    }

    public sealed class SlotRequest
    {
        public string StartTime { get; set; }
        public TimeSpan? Duration { get; set; }
    }
}

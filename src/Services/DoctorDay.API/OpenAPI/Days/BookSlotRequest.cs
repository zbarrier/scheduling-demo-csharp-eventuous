using System;

using static DoctorDay.Application.Commands.DayCommands;

namespace DoctorDay.API.OpenAPI.Days
{
    public sealed class BookSlotRequest
    {
        public Guid SlotId { get; set; }
        public string PatientId { get; set; }

        public BookSlot ToCommand(string dayId) => new(dayId, SlotId, PatientId);
    }
}

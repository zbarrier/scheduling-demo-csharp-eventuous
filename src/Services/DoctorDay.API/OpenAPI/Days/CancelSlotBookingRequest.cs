using System;

using static DoctorDay.Application.Commands.DayCommands;

namespace DoctorDay.API.OpenAPI.Days
{
    public sealed class CancelSlotBookingRequest
    {
        public Guid SlotId { get; set; }
        public string Reason { get; set; }

        public CancelSlotBooking ToCommand(string dayId) => new(dayId, SlotId, Reason);
    }
}

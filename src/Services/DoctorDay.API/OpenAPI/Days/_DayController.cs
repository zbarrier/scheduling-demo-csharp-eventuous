using DoctorDay.Application.Commands;
using DoctorDay.Application.Queries;
using DoctorDay.Domain.DayAggregate;

using Eventuous;

using Microsoft.AspNetCore.Mvc;

namespace DoctorDay.API.OpenAPI.Days
{
    [Route("api/v1")]
    [ApiController]
    public class DayController : ControllerBase
    {
        readonly IEventSerializer _eventSerializer;
        readonly IEventStore _eventStore;
        readonly DayCommandService _dayService;
        readonly IAvailableSlotsRepository _availableSlotsRepository;

        public DayController(IEventSerializer eventSerializer, IEventStore eventStore, 
            DayCommandService dayService, 
            IAvailableSlotsRepository availableSlotsRepository)
        {
            _eventSerializer = eventSerializer;
            _eventStore = eventStore;
            _dayService = dayService;
            _availableSlotsRepository = availableSlotsRepository;
        }

        [HttpGet]
        [Route("slots/today/available")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAvailableSlotsToday()
        {
            var availableSlots = (await _availableSlotsRepository.GetAvailableSlotsOn(DateTimeOffset.UtcNow.Date, default).ConfigureAwait(false))
                .Select(x => new
                {
                    Id = x.Id.Split('/').Last(),
                    DayId = x.DayId,
                    Date = x.Date,
                    StartTime = x.StartTime,
                    Duration = x.Duration
                });

            return Ok(availableSlots);
        }

        [HttpGet]
        [Route("slots/{date}/available")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAvailableSlots(string date)
        {
            if (!DateTimeOffset.TryParse(date, out DateTimeOffset parsedDate))
            {
                throw new ArgumentException(nameof(date));
            }

            var availableSlots = (await _availableSlotsRepository.GetAvailableSlotsOn(parsedDate, default).ConfigureAwait(false))
                .Select(x => new
                {
                    Id = x.Id.Split('/').Last(),
                    DayId = x.DayId,
                    Date = x.Date,
                    StartTime = x.StartTime,
                    Duration = x.Duration
                });

            return Ok(availableSlots);
        }

        [HttpPost]
        [Route("calendar/{date}/day-started")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CalendarDayStarted(string date)
        {
            if (!DateTimeOffset.TryParse(date, out DateTimeOffset parsedDate))
            {
                throw new ArgumentException(nameof(date));
            }

            var appendEventsResult = await _eventStore.AppendEvents(
                new StreamName("calendar_events"),
                ExpectedStreamVersion.Any,
                new List<StreamEvent>() { ToStreamEvent(new DayEvents.V1.CalendarDayStarted(parsedDate)) },
                default
            ).ConfigureAwait(false);

            return NoContent();


            StreamEvent ToStreamEvent(object evt)
            {
                var serializationResult = _eventSerializer.SerializeEvent(evt);
                var metadata = new Metadata();

                return new StreamEvent(Guid.NewGuid(), serializationResult.Payload, metadata, serializationResult.ContentType, -1);
            }
        }

        [HttpPost]
        [Route("doctor/schedule")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ScheduleDay([FromBody] ScheduleDayRequest scheduleDay)
        {
            var command = scheduleDay.ToCommand();

            var result = await _dayService.Handle(command, default);

            return Created($"/api/v1/slots/{command.Date.ToString("yyyy-MM-dd")}/available", result.State);
        }

        [HttpPut]
        [Route("slots/{dayId}/book")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> BookSlot(string dayId, [FromBody] BookSlotRequest bookSlot)
        {
            var command = bookSlot.ToCommand(dayId);

            var result = await _dayService.Handle(command, default);

            return NoContent();
        }

        [HttpPut]
        [Route("slots/{dayId}/cancel-booking")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelSlotBooking(string dayId, [FromBody] CancelSlotBookingRequest cancelSlotBooking)
        {
            var command = cancelSlotBooking.ToCommand(dayId);

            var result = await _dayService.Handle(command, default);

            return NoContent();
        }
    }
}

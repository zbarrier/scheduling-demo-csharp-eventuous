using DoctorDay.Application.Commands;
using DoctorDay.Domain.DayAggregate;

using Eventuous;
using Eventuous.TestHelpers;

using Microsoft.Extensions.Logging;

using static DoctorDay.Application.Commands.DayCommands;
using static DoctorDay.Domain.DayAggregate.DayEvents;

namespace DoctorDay.Domain.Tests.DayAggregate;

[Collection("TypeMapper collection")]
public class DayTests : AggregateTest<Day, DayState, DayId>
{
    readonly DoctorId _doctorId;
    readonly DateTimeOffset _date;
    readonly DayId _dayId;

    public DayTests(ILogger<DayCommandService> logger)
        : base((aggregateStore) => new DayCommandService(aggregateStore))
    {
        _doctorId = new DoctorId(Guid.NewGuid());
        _date = new DateTimeOffset(2023, 6, 1, 9, 0, 0, TimeSpan.Zero);
        _dayId = DayId.Create(_doctorId, _date);
    }

    [Fact]
    public async Task ScheduleDay_DayNotScheduled_DayScheduled()
    {
        var slotsToSchedule = Enumerable.Range(0, 30)
            .Select(r => new SlotToSchedule(_date.AddMinutes(10 * r), TimeSpan.FromMinutes(10)))
            .ToList();

        Given();

        await When(
            new ScheduleDay(_doctorId, _date, slotsToSchedule)
        );

        Then(changes =>
        {
            var dayScheduled = Assert.IsType<V1.DayScheduled>(changes.First().Event);
            Assert.Equal(_doctorId, dayScheduled.DoctorId);
            Assert.Equal(_date, dayScheduled.Date);
            Assert.Equal(31, changes.Count);
        });
    }

    [Fact]
    public async Task ScheduleDay_DayAlreadyScheduled_Error()
    {
        var slotsToSchedule = Enumerable.Range(0, 30)
            .Select(r => new SlotToSchedule(_date.AddMinutes(10 * r), TimeSpan.FromMinutes(10)))
            .ToList();

        Given(
            new V1.DayScheduled(_doctorId, _date)
        );

        await When(
            new ScheduleDay(_doctorId, _date, slotsToSchedule)
        );

        Then(DayExceptions.NewDayAlreadyScheduled());
    }

    [Fact]
    public async Task BookSlot_SlotScheduled_SlotBooked()
    {
        var slotId = new SlotId(Guid.NewGuid());
        var patientId = new PatientId("jdoe");

        Given(
            new V1.DayScheduled(_doctorId, _date),
            new V1.SlotScheduled(slotId, _date, TimeSpan.FromMinutes(10))
        );

        await When(
            new BookSlot(_dayId, slotId, patientId)
        );

        Then(changes => {
            var slotBooked = Assert.IsType<V1.SlotBooked>(Assert.Single(changes).Event);
            Assert.Equal(slotId, slotBooked.SlotId);
            Assert.Equal(patientId, slotBooked.PatientId);
        });
    }

    [Fact]
    public async Task BookSlot_DayNotScheduled_Error()
    {
        var slotId = new SlotId(Guid.NewGuid());
        var patientId = new PatientId("jdoe");

        Given();

        await When(
            new BookSlot(_dayId, slotId, patientId)
        );

        Then(DayExceptions.NewDayNotScheduled());
    }

    [Fact]
    public async Task BookSlot_SlotNotScheduled_Error()
    {
        var scheduledSlotId = new SlotId(Guid.NewGuid());
        var unScheduledSlotId = new SlotId(Guid.NewGuid());
        var patientId = new PatientId("jdoe");

        Given(
            new V1.DayScheduled(_doctorId, _date),
            new V1.SlotScheduled(scheduledSlotId, _date, TimeSpan.FromMinutes(10))
        );

        await When(
            new BookSlot(_dayId, unScheduledSlotId, patientId)
        );

        Then(DayExceptions.NewSlotNotScheduled());
    }

    [Fact]
    public async Task BookSlot_SlotAlreadyBooked_Error()
    {
        var slotId = new SlotId(Guid.NewGuid());
        var patientId = new PatientId("jdoe");

        Given(
            new V1.DayScheduled(_doctorId, _date),
            new V1.SlotScheduled(slotId, _date, TimeSpan.FromMinutes(10)),
            new V1.SlotBooked(slotId, patientId)
        );

        await When(
            new BookSlot(_dayId, slotId, patientId)
        );

        Then(DayExceptions.NewSlotAlreadyBooked());
    }

    [Fact]
    public async Task CancelSlotBooking_SlotBooked_BookingCanceled()
    {
        var slotId = new SlotId(Guid.NewGuid());
        var patientId = new PatientId("jdoe");
        var reason = "Cancel reason";

        Given(
            new V1.DayScheduled(_doctorId, _date),
            new V1.SlotScheduled(slotId, _date, TimeSpan.FromMinutes(10)),
            new V1.SlotBooked(slotId, patientId)
        );

        await When(
            new CancelSlotBooking(_dayId, slotId, reason)
        );

        Then(changes => {
            var slotBookingCancelled = Assert.IsType<V1.SlotBookingCancelled>(Assert.Single(changes).Event);
            Assert.Equal(slotId, slotBookingCancelled.SlotId);
            Assert.Equal(reason, slotBookingCancelled.Reason);
        });
    }

    [Fact]
    public async Task CancelSlotBooking_SlotNotBooked_Error()
    {
        var slotId = new SlotId(Guid.NewGuid());
        var reason = "Cancel reason";

        Given(
            new V1.DayScheduled(_doctorId, _date),
            new V1.SlotScheduled(slotId, _date, TimeSpan.FromMinutes(10))
        );

        await When(
            new CancelSlotBooking(_dayId, slotId, reason)
        );

        Then(DayExceptions.NewSlotNotBooked());
    }

    [Fact]
    public async Task ScheduleSlot_SlotNotScheduled_SlotScheduled()
    {
        var duration = TimeSpan.FromMinutes(10);

        Given(
            new V1.DayScheduled(_doctorId, _date)
        );

        await When(
            new ScheduleSlot(_dayId, _date, duration)
        );

        Then(changes => {
            var slotScheduled = Assert.IsType<V1.SlotScheduled>(Assert.Single(changes).Event);
            Assert.Equal(_date, slotScheduled.SlotStartTime);
            Assert.Equal(duration, slotScheduled.SlotDuration);
        });
    }

    [Fact]
    public async Task ScheduleSlot_OverlapsAnotherSlot_Error()
    {
        var slotId = new SlotId(Guid.NewGuid());
        var duration = TimeSpan.FromMinutes(10);

        Given(
            new V1.DayScheduled(_doctorId, _date),
            new V1.SlotScheduled(slotId, _date, TimeSpan.FromMinutes(10))
        );

        await When(
            new ScheduleSlot(_dayId, _date, duration)
        );

        Then(DayExceptions.NewSlotOverlaps());
    }

    [Fact]
    public async Task ScheduleSlot_IsAdjacentSlot_SlotScheduled()
    {
        var slotId = new SlotId(Guid.NewGuid());
        var startTime = _date.AddMinutes(10);
        var duration = TimeSpan.FromMinutes(10);

        Given(
            new V1.DayScheduled(_doctorId, _date),
            new V1.SlotScheduled(slotId, _date, TimeSpan.FromMinutes(10))
        );

        await When(
            new ScheduleSlot(_dayId, startTime, duration)
        );

        Then(changes => {
            var slotScheduled = Assert.IsType<V1.SlotScheduled>(Assert.Single(changes).Event);
            Assert.Equal(startTime, slotScheduled.SlotStartTime);
            Assert.Equal(duration, slotScheduled.SlotDuration);
        });
    }

    [Fact]
    public async Task CancelDaySchedule_DayScheduled_DayCancelled()
    {
        var slotOneId = new SlotId(Guid.NewGuid());
        var slotTwoId = new SlotId(Guid.NewGuid());

        Given(
            new V1.DayScheduled(_doctorId, _date),
            new V1.SlotScheduled(slotOneId, _date, TimeSpan.FromMinutes(10)),
            new V1.SlotScheduled(slotTwoId, _date.AddMinutes(10), TimeSpan.FromMinutes(10)),
            new V1.SlotBooked(slotTwoId, "jdoe")
        );

        await When(
            new CancelDaySchedule(_dayId)
        );

        Then(changes => {
            Assert.Equal(4, changes.Count);
            _ = Assert.IsType<V1.SlotBookingCancelled>(changes[0].Event);
            _ = Assert.IsType<V1.SlotScheduleCancelled>(changes[1].Event);
            _ = Assert.IsType<V1.SlotScheduleCancelled>(changes[2].Event);
            _ = Assert.IsType<V1.DayScheduleCancelled>(changes[3].Event);
        });
    }
}
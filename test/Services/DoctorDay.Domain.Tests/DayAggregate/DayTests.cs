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
}
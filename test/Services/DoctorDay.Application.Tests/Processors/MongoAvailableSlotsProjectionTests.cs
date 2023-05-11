using System.Diagnostics.Eventing.Reader;
using System.Diagnostics.Tracing;

using DockerComposeFixture;

using DoctorDay.Application.Processors;
using DoctorDay.Application.Queries;
using DoctorDay.Domain;
using DoctorDay.Infrastructure.MongoDB;
using DoctorDay.Infrastructure.RavenDB;

using Eventuous;
using Eventuous.Subscriptions;
using Eventuous.TestHelpers;

using MongoDB.Driver;

using Raven.Client.Documents;
using Raven.Client.ServerWide.Operations;

using static DoctorDay.Domain.DayAggregate.DayEvents;

namespace DoctorDay.Application.Tests.Processors;

[Collection("TypeMapper collection")]
public class MongoAvailableSlotsProjectionTests : HandlerTest, IClassFixture<DockerFixture>
{
    const string SubscriptionName = nameof(MongoAvailableSlotsProjectionTests);

    readonly DoctorId _doctorId;
    readonly DateTimeOffset _date;
    readonly DayId _dayId;
    readonly TimeSpan _tenMinutes;
    readonly DateTimeOffset _createdOn;

    IAvailableSlotsRepository _repository = default;

    public MongoAvailableSlotsProjectionTests(DockerFixture dockerFixture)
    {
        dockerFixture.Init(() => new DockerFixtureOptions()
        {
            DockerComposeFiles = new[] { "docker-compose-mongo.yml" },
            DebugLog = true,
            DockerComposeDownArgs = "--remove-orphans --volumes",
            CustomUpTest = output => output.Any(l => l.Contains("Waiting for connections"))
        });

        _doctorId = DoctorId.Create(Guid.NewGuid());
        _date = new DateTimeOffset(2023, 5, 9, 10, 0, 0, TimeSpan.Zero);
        _dayId = DayId.Create(_doctorId, _date);
        _tenMinutes = TimeSpan.FromMinutes(10);
        _createdOn = new DateTimeOffset(2023, 5, 1, 7, 0, 0, TimeSpan.Zero);

        EnableAtLeastOnceMonkey = false;
        EnableAtLeastOnceGorilla = false;
    }

    protected override IEventHandler GetHandler() => GetMongoHandler();

    IEventHandler GetMongoHandler()
    {
        _repository = new MongoAvailableSlotsRepository(new MongoClient("mongodb://localhost:28017"));
        return new AvailableSlotsProjection(_repository);
    }

    [Fact]
    public async Task Handle_SlotScheduled_ReadModelUpdated()
    {
        var slotId = SlotId.Create(Guid.NewGuid());
        var slotScheduled = new V1.SlotScheduled(slotId, _date, _tenMinutes);

        await Given(
            slotScheduled.AddMessageConsumeContext(new StreamName($"Day-{_dayId}"), 0, 5000, createdOnUtc: _createdOn)
        );

        Then(new List<ReadModels.AvailableSlot> {
                new ReadModels.AvailableSlot(
                    GetFullId(slotScheduled.SlotId),
                    _dayId,
                    slotScheduled.SlotStartTime.Date.ToString("yyyy-MM-dd"),
                    slotScheduled.SlotStartTime.ToString("h:mm tt"),
                    slotScheduled.SlotDuration
                )
                {
                    Position = 5000
                }
            }, await _repository.GetAvailableSlotsOn(_date, default));
    }

    string GetFullId(Guid slotId) => $"{slotId}";
}
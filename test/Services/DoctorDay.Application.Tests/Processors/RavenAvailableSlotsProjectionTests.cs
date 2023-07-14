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
using Raven.Client.Documents.Operations;
using Raven.Client.Exceptions.Database;
using Raven.Client.Exceptions;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

using static DoctorDay.Domain.DayAggregate.DayEvents;
using Raven.Client.Documents.Conventions;

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerClass, DisableTestParallelization = true)]

namespace DoctorDay.Application.Tests.Processors;

[Collection("TypeMapper collection")]
public class RavenAvailableSlotsProjectionTests : HandlerTest, IClassFixture<DockerFixture>
{
    const string SubscriptionName = nameof(RavenAvailableSlotsProjectionTests);

    const string RavenPrefix = "AvailableSlots";

    readonly DoctorId _doctorId;
    readonly DateTimeOffset _date;
    readonly DayId _dayId;
    readonly TimeSpan _tenMinutes;
    readonly DateTimeOffset _createdOn;

    IAvailableSlotsRepository _repository = default;

    public RavenAvailableSlotsProjectionTests(DockerFixture dockerFixture)
    {
        dockerFixture.InitOnce(() => new DockerFixtureOptions()
        {
            DockerComposeFiles = new[] { "docker-compose-raven.yml" },
            DebugLog = true,
            DockerComposeDownArgs = "--remove-orphans --volumes",
            CustomUpTest = output => output.Any(l => l.Contains("Running non-interactive."))
        });

        _doctorId = DoctorId.Create(Guid.NewGuid());
        _date = new DateTimeOffset(2023, 5, 9, 10, 0, 0, TimeSpan.Zero);
        _dayId = DayId.Create(_doctorId, _date);
        _tenMinutes = TimeSpan.FromMinutes(10);
        _createdOn = new DateTimeOffset(2023, 5, 1, 7, 0, 0, TimeSpan.Zero);

        EnableAtLeastOnceMonkey = false;
        EnableAtLeastOnceGorilla = false;
    }

    protected override IEventHandler GetHandler() => GetRavenHandler();

    IEventHandler GetRavenHandler()
    {
        var databaseName = $"DoctorDay_{Guid.NewGuid()}";

        var documentStore = new DocumentStore
        {
            Urls = new[] { "http://localhost:8180" },
            Database = databaseName,
        };

        documentStore.Conventions.AggressiveCache.Duration = TimeSpan.Zero;

        documentStore.Conventions.FindCollectionName = GetFindCollectionName;

        documentStore.Initialize();

        EnsureDatabaseExists(documentStore, databaseName, true);

        _repository = new RavenAvailableSlotsRepository(documentStore);

        return new AvailableSlotsProjection(_repository);
    }

    string GetFindCollectionName(Type type)
    {
        return type switch
        {
            _ => DocumentConventions.DefaultGetCollectionName(type)
        };
    }

    void EnsureDatabaseExists(IDocumentStore store, string? database, bool createDatabaseIfNotExists = true)
    {
        database = database ?? store.Database;

        if (string.IsNullOrWhiteSpace(database))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(database));

        try
        {
            store.Maintenance.ForDatabase(database).Send(new GetStatisticsOperation());
        }
        catch (DatabaseDoesNotExistException)
        {
            if (createDatabaseIfNotExists == false)
                throw;

            try
            {
                _ = store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(database)));
            }
            catch (ConcurrencyException)
            {
                // The database was already created before calling CreateDatabaseOperation
            }
        }
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
                    GetFullRavenId(slotScheduled.SlotId),
                    _dayId,
                    slotScheduled.SlotStartTime.Date.ToString("yyyy-MM-dd"),
                    slotScheduled.SlotStartTime.ToString("h:mm tt"),
                    slotScheduled.SlotDuration
                )
                {
                    Position = 5000
                }
            }, await _repository.GetAvailableSlotsOn(_date, default));

        //Cleanup
        await _repository.DeleteSlot(slotId, 5001, default);
    }

    [Fact]
    public async Task Handle_SlotBooked_ReadModelUpdated()
    {
        var slotId = SlotId.Create(Guid.NewGuid());
        var patientId = "jdoe";

        var slotScheduled = new V1.SlotScheduled(slotId, _date, _tenMinutes);
        var slotBooked = new V1.SlotBooked(slotId, patientId);

        await Given(
            slotScheduled.AddMessageConsumeContext(new StreamName($"Day-{_dayId}"), 0, 5000, createdOnUtc: _createdOn),
            slotBooked.AddMessageConsumeContext(new StreamName($"Day-{_dayId}"), 1, 5100, createdOnUtc: _createdOn.AddHours(1))
        );

        Then(new List<ReadModels.AvailableSlot>() { }, 
            await _repository.GetAvailableSlotsOn(_date, default)
        );

        //Cleanup
        await _repository.DeleteSlot(slotId, 5101, default);
    }

    [Fact]
    public async Task Handle_SlotBookingCancelled_ReadModelUpdated()
    {
        var slotId = SlotId.Create(Guid.NewGuid());
        var patientId = "jdoe";
        var reason = "Conflict.";

        var slotScheduled = new V1.SlotScheduled(slotId, _date, _tenMinutes);
        var slotBooked = new V1.SlotBooked(slotId, patientId);
        var slotBookingCancelled = new V1.SlotBookingCancelled(slotId, reason);

        await Given(
            slotScheduled.AddMessageConsumeContext(new StreamName($"Day-{_dayId}"), 0, 5000, createdOnUtc: _createdOn),
            slotBooked.AddMessageConsumeContext(new StreamName($"Day-{_dayId}"), 1, 5100, createdOnUtc: _createdOn.AddHours(1)),
            slotBookingCancelled.AddMessageConsumeContext(new StreamName($"Day-{_dayId}"), 3, 5200, createdOnUtc: _createdOn.AddHours(2))
        );

        Then(new List<ReadModels.AvailableSlot> {
                new ReadModels.AvailableSlot(
                    GetFullRavenId(slotScheduled.SlotId),
                    _dayId,
                    slotScheduled.SlotStartTime.Date.ToString("yyyy-MM-dd"),
                    slotScheduled.SlotStartTime.ToString("h:mm tt"),
                    slotScheduled.SlotDuration
                )
                {
                    Position = 5200
                }
            }, await _repository.GetAvailableSlotsOn(_date, default));

        //Cleanup
        await _repository.DeleteSlot(slotId, 5201, default);
    }

    string GetFullRavenId(Guid slotId) => $"{RavenPrefix}/{slotId}";
}
using BuildingBlocks.Archivers;

using DoctorDay.Application.Commands;
using DoctorDay.Application.Queries;
using DoctorDay.Domain;
using DoctorDay.Domain.DayAggregate;

using Eventuous;
using Eventuous.Producers;
using Eventuous.Subscriptions.Context;

using Microsoft.Extensions.Options;

namespace DoctorDay.Application.Processors;

public sealed class DayArchiverProcessManagerOptions
{
    public TimeSpan Threshold { get; set; }
    public string QueueName { get; set; }
}

public sealed class DayArchiverProcessManager : Eventuous.Subscriptions.EventHandler
{
    readonly DayArchiverProcessManagerOptions _options;
    readonly IArchivableDaysRepository _repository;
    readonly IEventProducer _producer;
    readonly IEventSerializer _serializer;
    readonly IEventStore _eventStore;
    readonly IColdStorage _coldStorage;

    public DayArchiverProcessManager(IOptions<DayArchiverProcessManagerOptions> options,
        IArchivableDaysRepository repository,
        IEventProducer producer,
        IEventSerializer serializer,
        IEventStore eventStore,
        IColdStorage coldStorage)
    {
        _options = options.Value;
        _repository = repository;
        _producer = producer;
        _serializer = serializer;
        _eventStore = eventStore;
        _coldStorage = coldStorage;

        On<DayEvents.DayScheduled_V1>(HandleEvent);
        On<DayEvents.CalendarDayStarted_V1>(HandleEvent);
        On<DayEvents.DayScheduleArchived_V1>(HandleEvent);
    }

    async ValueTask HandleEvent(MessageConsumeContext<DayEvents.DayScheduled_V1> context)
        => await _repository.Add(new ReadModels.ArchivableDay(context.Stream.GetId(), context.Message.Date), context.CancellationToken).ConfigureAwait(false);

    async ValueTask HandleEvent(MessageConsumeContext<DayEvents.CalendarDayStarted_V1> context)
    {
        var archivableDays = await _repository
            .FindAll(context.Message.Date.Add(_options.Threshold), context.CancellationToken)
            .ConfigureAwait(false);

        var archiveDayScheduleCmds = archivableDays
            .Select(x => new DayCommands.ArchiveDaySchedule(x.Id))
            .ToList()
            .AsEnumerable();

        var queueName = new StreamName(_options.QueueName);

        var metadata = Metadata.FromMeta(context.Metadata);
        _ = metadata.AddNotNull(MetaTags.CausationId, context.MessageId);

        await _producer.Produce<IEnumerable<DayCommands.ArchiveDaySchedule>>(queueName, archiveDayScheduleCmds, context.Metadata, 
            cancellationToken: context.CancellationToken).ConfigureAwait(false);
    }

    async ValueTask HandleEvent(MessageConsumeContext<DayEvents.DayScheduleArchived_V1> context)
    {
        var streamName = context.Stream;
        var eventsToArchive = new List<StreamEvent>();
        var lastPosition = 0L;

        var streamEvents = await _eventStore.ReadStream(
            streamName,
            StreamReadPosition.Start,
            true,
            context.CancellationToken
        ).ConfigureAwait(false);

        foreach (var se in streamEvents)
        {
            if (se is not null)
            {
                eventsToArchive.Add(se);
                lastPosition = se.Position;
            }
        }

        await _coldStorage.ArchiveStream(streamName, eventsToArchive, context.CancellationToken)
            .ConfigureAwait(false);

        await _eventStore.TruncateStream(
            streamName,
            new StreamTruncatePosition(lastPosition),
            ExpectedStreamVersion.Any,
            context.CancellationToken
        ).ConfigureAwait(false);
    }
}

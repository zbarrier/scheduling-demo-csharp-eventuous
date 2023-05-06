using DoctorDay.Application.Commands;
using System.Threading;

using DoctorDay.Application.Queries;
using DoctorDay.Domain;
using DoctorDay.Domain.DayAggregate;

using Eventuous.Producers;
using Eventuous.Subscriptions;
using Eventuous.Subscriptions.Context;

using Microsoft.Extensions.Options;
using Eventuous;

namespace DoctorDay.Application.Processors;

public sealed class OverbookingProcessManagerOptions
{
    public int BookingLimitPerPatient { get; set; }
    public string QueueName { get; set; }
}

public sealed class OverbookingProcessManager : Eventuous.Subscriptions.EventHandler
{
    readonly OverbookingProcessManagerOptions _options;
    readonly IBookedSlotsRepository _repository;
    readonly IEventProducer _producer;
    readonly TypeMapper _typeMapper;

    public OverbookingProcessManager(IOptions<OverbookingProcessManagerOptions> options,
        IBookedSlotsRepository repository,
        IEventProducer producer,
        TypeMapper typeMapper)
    {
        _options = options.Value;
        _repository = repository;
        _producer = producer;
        _typeMapper = typeMapper;

        On<DayEvents.SlotScheduled_V1>(HandleEvent);
        On<DayEvents.SlotBooked_V1>(HandleEvent);
    }

    public string DiagosticName => nameof(OverbookingProcessManager);

    async ValueTask HandleEvent(MessageConsumeContext<DayEvents.SlotScheduled_V1> context)
    {
        var evt = context.Message;

        await _repository.AddSlot(
            new ReadModels.BookedSlot(evt.SlotId.ToString(), context.Stream.GetId(), evt.SlotStartTime.Year, evt.SlotStartTime.Month)
            {
                Position = context.GlobalPosition
            },
            context.CancellationToken
            ).ConfigureAwait(false);
    }

    async ValueTask HandleEvent(MessageConsumeContext<DayEvents.SlotBooked_V1> context)
    {
        var evt = context.Message;

        var slot = await _repository.MarkSlotAsBooked(evt.SlotId, evt.PatientId, context.GlobalPosition, context.CancellationToken)
            .ConfigureAwait(false);
        var numOfSlotsBookedByPatientThisMonth = await _repository.CountByPatientAndYearAndMonth(evt.PatientId, slot.Year, slot.Month, context.CancellationToken)
            .ConfigureAwait(false);

        //Console.WriteLine($"Number of booked slots for patient: {numOfSlotsBookedByPatientThisMonth}");

        if (numOfSlotsBookedByPatientThisMonth > _options.BookingLimitPerPatient)
        {
            var queueName = new StreamName(_options.QueueName);
            var cancelSlotBooking = new DayCommands.CancelSlotBooking(context.Stream.GetId(), evt.SlotId, "Patient exceeded monthly booking limit.");

            var metadata = Metadata.FromMeta(context.Metadata);
            _ = metadata.AddNotNull(MetaTags.CausationId, context.MessageId);

            await _producer.Produce<DayCommands.CancelSlotBooking>(queueName, cancelSlotBooking, metadata, cancellationToken: context.CancellationToken);
        }
    }
}

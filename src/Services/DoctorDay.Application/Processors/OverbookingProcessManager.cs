using DoctorDay.Application.Commands;
using DoctorDay.Application.Queries;
using DoctorDay.Domain;

using Eventuous;
using Eventuous.Producers;
using Eventuous.Subscriptions.Context;

using Microsoft.Extensions.Options;

using static DoctorDay.Domain.DayAggregate.DayEvents;

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

    public OverbookingProcessManager(IOptions<OverbookingProcessManagerOptions> options,
        IBookedSlotsRepository repository,
        IEventProducer producer)
    {
        _options = options.Value;
        _repository = repository;
        _producer = producer;

        On<V1.SlotScheduled>(HandleEvent);
        On<V1.SlotBooked>(HandleEvent);
    }

    async ValueTask HandleEvent(MessageConsumeContext<V1.SlotScheduled> context)
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

    async ValueTask HandleEvent(MessageConsumeContext<V1.SlotBooked> context)
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

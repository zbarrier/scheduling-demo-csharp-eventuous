using DoctorDay.Application.Queries;
using DoctorDay.Domain;

using Eventuous.Subscriptions;
using Eventuous.Subscriptions.Context;

using static DoctorDay.Domain.DayAggregate.DayEvents;

namespace DoctorDay.Application.Processors;
public sealed class AvailableSlotsProjection : IEventHandler
{
    readonly IAvailableSlotsRepository _repository;

    public AvailableSlotsProjection(IAvailableSlotsRepository availableSlotsRepository)
        => _repository = availableSlotsRepository;

    public string DiagnosticName => nameof(AvailableSlotsProjection);

    public async ValueTask<EventHandlingStatus> HandleEvent(IMessageConsumeContext context)
        => context.Message switch
        {
            V1.SlotScheduled evt =>
                await _repository.AddSlot(
                    new ReadModels.AvailableSlot(
                        evt.SlotId.ToString(),
                        context.Stream.GetId(),
                        evt.SlotStartTime.ToString("dd-MM-yyyy"),
                        evt.SlotStartTime.ToString("h:mm tt"),
                        evt.SlotDuration)
                    {
                        Position = context.GlobalPosition
                    },
                    context.CancellationToken
                ).ConfigureAwait(false),

            V1.SlotBooked evt => await _repository.HideSlot(evt.SlotId, context.GlobalPosition, context.CancellationToken).ConfigureAwait(false),
            V1.SlotBookingCancelled evt => await _repository.ShowSlot(evt.SlotId, context.GlobalPosition, context.CancellationToken).ConfigureAwait(false),
            V1.SlotScheduleCancelled evt => await _repository.DeleteSlot(evt.SlotId, context.GlobalPosition, context.CancellationToken).ConfigureAwait(false),

            _ => EventHandlingStatus.Ignored,
        };
}

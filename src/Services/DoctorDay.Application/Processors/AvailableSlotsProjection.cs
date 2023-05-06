using DoctorDay.Application.Queries;
using DoctorDay.Domain;
using DoctorDay.Domain.DayAggregate;

using Eventuous;
using Eventuous.Subscriptions;
using Eventuous.Subscriptions.Context;

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
            DayEvents.SlotScheduled_V1 evt =>
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

            DayEvents.SlotBooked_V1 evt => await _repository.HideSlot(evt.SlotId, context.GlobalPosition, context.CancellationToken).ConfigureAwait(false),
            DayEvents.SlotBookingCancelled_V1 evt => await _repository.ShowSlot(evt.SlotId, context.GlobalPosition, context.CancellationToken).ConfigureAwait(false),
            DayEvents.SlotScheduleCancelled_V1 evt => await _repository.DeleteSlot(evt.SlotId, context.GlobalPosition, context.CancellationToken).ConfigureAwait(false),

            _ => EventHandlingStatus.Success,
        };
}

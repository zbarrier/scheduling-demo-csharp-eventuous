using Eventuous;
using Eventuous.Subscriptions.Context;

using Microsoft.Extensions.Logging;

namespace DoctorDay.Application.Commands;

public sealed class DayCommandsAsyncHandler : Eventuous.Subscriptions.EventHandler
{
    public const string QueueName = "doctorday_async_cmds";

    public static readonly StreamName Stream = new(QueueName);

    readonly ILogger<DayCommandsAsyncHandler> _logger;
    readonly DayCommandService _dayService;

    public DayCommandsAsyncHandler(ILogger<DayCommandsAsyncHandler> logger, DayCommandService dayService)
    {
        _logger = logger;
        _dayService = dayService;

        On<DayCommands.ArchiveDaySchedule>(HandleEvent);
        On<DayCommands.CancelSlotBooking>(HandleEvent);
    }

    async ValueTask HandleEvent(MessageConsumeContext<DayCommands.ArchiveDaySchedule> context)
        => await _dayService.Handle(context.Message, context.CancellationToken).ConfigureAwait(false);

    async ValueTask HandleEvent(MessageConsumeContext<DayCommands.CancelSlotBooking> context)
        => await _dayService.Handle(context.Message, context.CancellationToken).ConfigureAwait(false);
}

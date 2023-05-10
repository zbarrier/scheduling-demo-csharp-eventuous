using Eventuous.Subscriptions;
using Eventuous.Subscriptions.Context;

using Xunit;

namespace Eventuous.TestHelpers;
public abstract class HandlerTest
{
    protected bool EnableAtLeastOnceMonkey { get; set; }
    protected bool EnableAtLeastOnceGorilla { get; set; }

    protected abstract IEventHandler GetHandler();

    protected async Task Given(params MessageConsumeContext[] events)
    {
        IEventHandler eventHandler = GetHandler();

        foreach (var evt in events)
        {
            await eventHandler.HandleEvent(evt);

            if (EnableAtLeastOnceMonkey)
                await eventHandler.HandleEvent(evt);
        }

        if (EnableAtLeastOnceGorilla)
        {
            foreach (var evt in events.Take(events.Length - 1))
            {
                await eventHandler.HandleEvent(evt);
            }
        }
    }

    protected void Then(object expected, object actual) => Assert.Equal(expected, actual);
}
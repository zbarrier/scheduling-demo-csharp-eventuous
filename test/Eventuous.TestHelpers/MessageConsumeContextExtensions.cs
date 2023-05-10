using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Eventuous;
using Eventuous.Subscriptions.Context;

namespace Eventuous.TestHelpers
{
    public static class MessageConsumeContextExtensions
    {
        public const string JsonContentType = "application/json";
        public const string SubscriptionName = "Test";

        public static MessageConsumeContext AddMessageConsumeContext(this object @event, StreamName stream, ulong streamPosition, ulong globalPosition, 
            ulong? sequence = null,
            Metadata? metadata = null, 
            DateTimeOffset? createdOnUtc = null)
        {
            return new MessageConsumeContext(
                Guid.NewGuid().ToString(),
                @event.GetType().Name,
                JsonContentType,
                stream,
                streamPosition,
                globalPosition,
                sequence ?? 0,
                createdOnUtc?.DateTime ?? DateTimeOffset.UtcNow.Date,
                @event,
                metadata ?? NewMetadata(Guid.NewGuid(), Guid.NewGuid()),
                SubscriptionName,
                default);
        }

        public static Metadata NewMetadata(Guid correlationId, Guid causationId)
            => new Metadata()
            {
                { MetaTags.CorrelationId, correlationId },
                { MetaTags.CausationId, causationId }
            };
    }
}

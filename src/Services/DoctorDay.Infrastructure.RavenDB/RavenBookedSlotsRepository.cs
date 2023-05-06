using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DoctorDay.Application.Queries;
using DoctorDay.Domain;

using Eventuous.Subscriptions;

using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Session;

namespace DoctorDay.Infrastructure
{
    public sealed class RavenBookedSlotsRepository : IBookedSlotsRepository
    {
        readonly IDocumentStore _documentStore;
        readonly SessionOptions _noTrackingAndNoCachingOptions = new SessionOptions { NoTracking = true, NoCaching = true };
        readonly string _prefix;

        public RavenBookedSlotsRepository(IDocumentStore documentStore)
        {
            _documentStore = documentStore;

            var pluralisedName = DocumentConventions.DefaultGetCollectionName(typeof(ReadModels.BookedSlot));
            _prefix = _documentStore.Conventions.TransformTypeCollectionNameToDocumentIdPrefix(pluralisedName);
        }

        public async Task<int> CountByPatientAndYearAndMonth(string patientId, int year, int month, CancellationToken cancellationToken)
        {
            using var session = _documentStore.OpenAsyncSession(_noTrackingAndNoCachingOptions);

            var count = await session
                .Query<ReadModels.BookedSlot>()
                .Customize(x => x.WaitForNonStaleResults(TimeSpan.FromSeconds(5)))
                .Where(x => x.IsBooked && x.PatientId == patientId)
                .Where(x => x.Year == year && x.Month == month)
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            return count;
        }

        public async Task AddSlot(ReadModels.BookedSlot slot, CancellationToken cancellationToken)
        {
            using var session = _documentStore.OpenAsyncSession();

            var slotFullId = GetFullId(slot.Id);

            bool slotDoesNotExist = !await session.Advanced.ExistsAsync(slotFullId, cancellationToken).ConfigureAwait(false);

            if (slotDoesNotExist)
            {
                await session.StoreAsync(slot, slotFullId, cancellationToken).ConfigureAwait(false);
                await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        public async Task MarkSlotAsAvailable(Guid slotId, ulong position, CancellationToken cancellationToken)
        {
            using var session = _documentStore.OpenAsyncSession();

            var slot = await session
                .LoadAsync<ReadModels.BookedSlot>(GetFullId(slotId), cancellationToken)
                .ConfigureAwait(false);

            bool eventHasNotBeenHandled = position > slot.Position;

            if (eventHasNotBeenHandled)
            {
                slot.IsBooked = false;
                slot.PatientId = null;
                slot.Position = position;

                await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        public async Task<ReadModels.BookedSlot> MarkSlotAsBooked(Guid slotId, string patientId, ulong position, CancellationToken cancellationToken)
        {
            using var session = _documentStore.OpenAsyncSession();

            var slot = await session
                .LoadAsync<ReadModels.BookedSlot>(GetFullId(slotId), cancellationToken)
                .ConfigureAwait(false);

            bool eventHasNotBeenHandled = position > slot.Position;

            if (eventHasNotBeenHandled)
            {
                slot.IsBooked = true;
                slot.PatientId = patientId;
                slot.Position = position;

                await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            return slot;
        }

        string GetFullId(Guid shortId) => $"{_prefix}/{shortId}";
        string GetFullId(string shortId) => $"{_prefix}/{shortId}";
    }
}

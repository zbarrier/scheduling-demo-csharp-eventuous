using Eventuous;

namespace BuildingBlocks.Archivers;
public interface IColdStorage
{
    Task ArchiveStream(string streamName, IEnumerable<StreamEvent> events, CancellationToken cancellationToken);
}

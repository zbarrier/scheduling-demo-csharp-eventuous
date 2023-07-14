using Eventuous.TestHelpers;

using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;

namespace Eventuous.Projections.RavenDB.TestUtils;
public abstract class RavenHandlerTest : HandlerTest
{
    protected DocumentStore _documentStore;

    protected DocumentStore GetRavenDocumentStore(int port)
    {
        var databaseName = $"IntegrationTests_{Guid.NewGuid()}";

        var _documentStore = new DocumentStore
        {
            Urls = new[] { "http://localhost:8180" },
            Database = databaseName,
        };

        _documentStore.Conventions.AggressiveCache.Duration = TimeSpan.Zero;

        _documentStore.Conventions.FindCollectionName = GetFindCollectionName;

        _documentStore.Initialize();

        _documentStore.EnsureRavenDBExists(databaseName, true);

        return _documentStore;
    }

    protected virtual string GetFindCollectionName(Type type)
    {
        return type switch
        {
            _ => DocumentConventions.DefaultGetCollectionName(type)
        };
    }
}

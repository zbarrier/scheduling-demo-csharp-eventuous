using Raven.Client.Documents.Operations;
using Raven.Client.ServerWide.Operations;

namespace Raven.Client.Documents;

public static class SetupExtensions
{
    public static void EnsureRavenDBExists(this IDocumentStore store, string? database, bool createIfNotExists = true)
    {
        database ??= store.Database;

        if (string.IsNullOrWhiteSpace(database))
        {
            throw new ArgumentNullException(nameof(database), "Database name cannot be null or whitespace.");
        }

        try
        {
            store.Maintenance.ForDatabase(database).Send(new GetStatisticsOperation());
        }
        catch (Exception ex)
        {
            if (!createIfNotExists)
            {
                throw;
            }

            try
            {
                _ = store.Maintenance.Server.Send(new CreateDatabaseOperation(new Raven.Client.ServerWide.DatabaseRecord(database)));
            }
            catch (Exception)
            {
                // Database already exists.
            }
        }
    }
}

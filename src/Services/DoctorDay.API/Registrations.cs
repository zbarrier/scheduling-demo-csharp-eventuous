using DoctorDay.Application.Commands;
using DoctorDay.Application.Queries;
using DoctorDay.Domain.DayAggregate;
using DoctorDay.Infrastructure.RavenDB;
using EventStore.Client;

using Eventuous;
using Eventuous.EventStore;

using Raven.Client.Documents;

using System.Text.Json;

namespace DoctorDay.API;

public static class Registrations
{
    public static IServiceCollection AddEventStoreDB(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        DefaultEventSerializer.SetDefaultSerializer(new DefaultEventSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web), TypeMap.Instance));
        _ = services.AddSingleton(DefaultEventSerializer.Instance);

        _ = services.AddSingleton<EventStoreClient>((provider) => {
                var settings = EventStoreClientSettings.Create(configuration["EventStoreDB:ConnectionString"]);
                settings.ConnectionName = configuration["EventStoreDB:ConnectionName"];
                settings.DefaultCredentials = new UserCredentials(
                    configuration["EventStoreDB:UserCredentials:Username"]!,
                    configuration["EventStoreDB:UserCredentials:Password"]!
                );

                return new EventStoreClient(settings);
            })
            .AddAggregateStore<EsdbEventStore>()
            .AddSingleton<IEventSerializer, DefaultEventSerializer>()
            .AddCommandService<DayCommandService, Day>();

        return services;
    }

    public static IServiceCollection AddRavenDB(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        _ = services.AddSingleton<IDocumentStore>((provider) => 
            {
                var documentStore = new DocumentStore
                {
                    Urls = configuration["RavenDB:Server"].Split(',', System.StringSplitOptions.RemoveEmptyEntries),
                    Database = configuration["RavenDB:Database"],
                    Conventions = 
                    {
                        AggressiveCache = 
                        {
                            Duration = TimeSpan.FromDays(1),
                            Mode = Raven.Client.Http.AggressiveCacheMode.TrackChanges
                        }
                    }
                }.Initialize();

                documentStore.AggressivelyCache();

                return documentStore;
            })
            .AddSingleton<IAvailableSlotsRepository, RavenAvailableSlotsRepository>();

        return services;
    }
}

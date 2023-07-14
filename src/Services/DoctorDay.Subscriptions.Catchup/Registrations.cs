using System.Text.Json;

using BuildingBlocks.Archivers;
using BuildingBlocks.Archivers.Azure;
using BuildingBlocks.Projections.RavenDB;

using DoctorDay.Application.Commands;
using DoctorDay.Application.Processors;
using DoctorDay.Application.Queries;
using DoctorDay.Domain.DayAggregate;
using DoctorDay.Infrastructure;
using DoctorDay.Infrastructure.RavenDB;

using EventStore.Client;

using Eventuous;
using Eventuous.EventStore;
using Eventuous.EventStore.Producers;
using Eventuous.EventStore.Subscriptions;
using Eventuous.Producers;

using Raven.Client.Documents;

namespace DoctorDay.Subscriptions.Catchup;
public static class Registrations
{
    public static IServiceCollection AddColdStorage(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        _ = services.AddSingleton<IColdStorage, AzureBlobColdStorage>();

        return services;
    }

    public static IServiceCollection AddEventStoreDB(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        DefaultEventSerializer.SetDefaultSerializer(new DefaultEventSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web), TypeMap.Instance));
        _ = services.AddSingleton(DefaultEventSerializer.Instance);

        _ = services.AddSingleton<EventStoreClient>((provider) => 
            {
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

    public static IServiceCollection AddProducers(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddEventProducer<EventStoreProducer>((provider) => 
            {
                var client = provider.GetRequiredService<EventStoreClient>();
                var serializer = provider.GetRequiredService<IEventSerializer>();

                return new EventStoreProducer(client, serializer);
            });

        return services;
    }

    public static IServiceCollection AddRavenDB(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        _ = services.AddSingleton<IDocumentStore>((provider) =>
            {
                var documentStore = new DocumentStore
                {
                    Urls = configuration["RavenDB:Server"]!.Split(',', System.StringSplitOptions.RemoveEmptyEntries),
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

                if (environment.IsDevelopment())
                {
                    SetupExtensions.EnsureRavenDBExists(documentStore, configuration["RavenDB:Database"], true);
                }

                _ = documentStore.AggressivelyCache();

                return documentStore;
            })
            .AddSingleton<RavenCheckpointStore>((provider) =>
                new RavenCheckpointStore(
                    provider.GetRequiredService<ILogger<RavenCheckpointStore>>(),
                    provider.GetRequiredService<IDocumentStore>(),
                    batchSize: 5
                )
            )
            .AddSingleton<IAvailableSlotsRepository, RavenAvailableSlotsRepository>()
            .AddSingleton<IArchivableDaysRepository, RavenArchivableDaysRepository>()
            .AddSingleton<IBookedSlotsRepository, RavenBookedSlotsRepository>();

        return services;
    }

    public static IServiceCollection AddSubscriptions(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSubscription<AllStreamSubscription, AllStreamSubscriptionOptions>(
                "RavenDayProjections",
                builder =>
                {
                    builder.ConfigureOptions(new AllStreamSubscriptionOptions()
                    {
                        EventFilter = StreamFilter.Prefix("Day-", "calendar_events"),
                    });

                    builder.UseCheckpointStore<RavenCheckpointStore>()
                        .AddEventHandler<AvailableSlotsProjection>()
                        .AddEventHandler<DayArchiverProcessManager>()
                        .AddEventHandler<OverbookingProcessManager>();
                }
            );

        return services;
    }
}

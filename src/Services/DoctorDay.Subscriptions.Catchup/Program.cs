using System.Text.Json;

using Autofac;
using Autofac.Extensions.DependencyInjection;

using BuildingBlocks.Archivers;
using BuildingBlocks.Archivers.Azure;
using BuildingBlocks.JsonConverters;
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

public class Program
{
    public static void Main(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder(args)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureServices((hostContext, services) =>
            {
                IConfiguration configuration = hostContext.Configuration;

                _ = services.AddOptions()
                            .Configure<AzureBlobColdStorageOptions>(configuration.GetSection("AzureBlobColdStorage"))
                            .Configure<DayArchiverProcessManagerOptions>(configuration.GetSection("DayArchiverProcessManager"))
                            .Configure<OverbookingProcessManagerOptions>(configuration.GetSection("OverbookingProcessManager"));

                _ = hostContext.HostingEnvironment.IsDevelopment()
                    ? services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace))
                    : services.AddLogging();

                //Serialization
                _ = services.AddSingleton<System.Text.Json.JsonSerializerOptions>((provider) => 
                            {
                                var options = new System.Text.Json.JsonSerializerOptions();
                                options.Converters.Add(new TimeSpanConverter());
                                return options;
                            });
                DefaultEventSerializer.SetDefaultSerializer(new DefaultEventSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web)));

                //Event Mappings
                TypeMap.RegisterKnownEventTypes();

                //EventStoreDB
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
                            .AddCommandService<DayCommandService, Day>()
                            .AddSingleton<IEventSerializer, DefaultEventSerializer>();

                //RavenDB
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

                //Cold Storage
                _ = services.AddSingleton<IColdStorage, AzureBlobColdStorage>();

                //Subscriptions
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

                //Producers
                services.AddEventProducer<EventStoreProducer>((provider) => {
                            var client = provider.GetRequiredService<EventStoreClient>();
                            var serializer = provider.GetRequiredService<IEventSerializer>();

                            return new EventStoreProducer(client, serializer);
                        });
            })
            .ConfigureContainer<ContainerBuilder>((context, builder) => {

            })
            .Build();

        host.Run();
    }
}
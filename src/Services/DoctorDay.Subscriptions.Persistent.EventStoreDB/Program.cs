using System.Text.Json;

using Autofac;
using Autofac.Extensions.DependencyInjection;

using BuildingBlocks.JsonConverters;

using DoctorDay.Application.Commands;
using DoctorDay.Domain.DayAggregate;

using EventStore.Client;

using Eventuous;
using Eventuous.EventStore;
using Eventuous.EventStore.Subscriptions;

using Microsoft.Extensions.Options;

namespace DoctorDay.Subscriptions.Persistent.EventStoreDB;

public class Program
{
    public static void Main(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder(args)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureServices((hostContext, services) =>
            {
                var configuration = hostContext.Configuration;
                var hostingEnv = hostContext.HostingEnvironment;

                _ = hostingEnv.IsDevelopment()
                    ? services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace))
                    : services.AddLogging();

                //Serialization
                _ = services.AddSingleton<System.Text.Json.JsonSerializerOptions>((provider) => {
                    var options = new System.Text.Json.JsonSerializerOptions();
                    options.Converters.Add(new TimeSpanConverter());
                    return options;
                });
                DefaultEventSerializer.SetDefaultSerializer(new DefaultEventSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web)));

                //Event Mappings
                DayEvents.MapDayEvents();

                //EventStoreDB
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
                            .AddCommandService<DayCommandService, Day>()
                            .AddSingleton<IEventSerializer, DefaultEventSerializer>();

                //Subscriptions
                services.AddSubscription<StreamPersistentSubscription, StreamPersistentSubscriptionOptions>(
                    "DayCommandsAsyncHandler",
                    builder => builder
                        .Configure(x => x.StreamName = DayCommandsAsyncHandler.Stream)
                        .AddEventHandler<DayCommandsAsyncHandler>()
                );
            })
            .ConfigureContainer<ContainerBuilder>((context, builder) => {

            })
            .Build();

        host.Run();
    }
}
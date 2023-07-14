using System.Diagnostics;

using Autofac;
using Autofac.Extensions.DependencyInjection;

using BuildingBlocks.Archivers.Azure;
using BuildingBlocks.CombGuid;

using DoctorDay.Application.Processors;

using Eventuous;

using Serilog;

namespace DoctorDay.Subscriptions.Catchup;

public class Program
{
    static readonly string AppName = typeof(Program).Namespace!.Replace('.', '-');

    public static void Main(string[] args)
    {
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            CombGuid.Initialize();

            TypeMap.RegisterKnownEventTypes();

            IHost host = Host.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration configuration = hostContext.Configuration;
                    IHostEnvironment environment = hostContext.HostingEnvironment;

                    _ = services
                        .AddOptions()
                        .Configure<AzureBlobColdStorageOptions>(configuration.GetSection("AzureBlobColdStorage"))
                        .Configure<DayArchiverProcessManagerOptions>(configuration.GetSection("DayArchiverProcessManager"))
                        .Configure<OverbookingProcessManagerOptions>(configuration.GetSection("OverbookingProcessManager"));

                    _ = hostContext.HostingEnvironment.IsDevelopment()
                        ? services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace))
                        : services.AddLogging();

                    //Event Mappings
                    TypeMap.RegisterKnownEventTypes();

                    _ = services
                        .AddColdStorage(configuration, environment)
                        .AddEventStoreDB(configuration, environment)
                        .AddProducers(configuration, environment)
                        .AddRavenDB(configuration, environment)
                        .AddSubscriptions(configuration, environment);
                })
                .ConfigureContainer<ContainerBuilder>((context, builder) =>
                {
                    IConfiguration configuration = context.Configuration;
                    IHostEnvironment environment = context.HostingEnvironment;
                })
                .UseSerilog((context, services, configuration) =>
                {
                    configuration
                        .ReadFrom.Configuration(context.Configuration)
                        .ReadFrom.Services(services);
                })
                .Build();

            host.Run();
        }
        catch (Exception ex) 
        {
            Log.Fatal(ex, "Program {AppName} terminated unexpectedly!", AppName);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
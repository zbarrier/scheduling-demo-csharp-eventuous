using System.Diagnostics;

using Autofac;
using Autofac.Extensions.DependencyInjection;

using BuildingBlocks.CombGuid;
using BuildingBlocks.JsonConverters;

using Eventuous;

using Google.Protobuf.WellKnownTypes;

using Microsoft.OpenApi.Models;

using Serilog;

namespace DoctorDay.API;

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
            Log.Information("Configuring and starting web host for {AppName}.", AppName);

            CombGuid.Initialize();

            TypeMap.RegisterKnownEventTypes(typeof(DoctorDay.Domain.DayAggregate.DayEvents.V1.DayScheduled).Assembly);

            var builder = WebApplication.CreateBuilder(args);

            builder.Host
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureServices((context, services) =>
                {
                    var configuration = context.Configuration;
                    var environment = context.HostingEnvironment;

                    _ = services.AddControllers()
                        .AddJsonOptions(opts => {
                            opts.JsonSerializerOptions.Converters.Add(new TimeSpanConverter());
                        });

                    _ = services.AddEndpointsApiExplorer()
                        .AddSwaggerGen(c => 
                        {
                            c.SwaggerDoc("v1", new OpenApiInfo { Title = "DoctorDay.API", Version = "v1" });
                        });

                    services.AddEventStoreDB(configuration, environment);
                    services.AddRavenDB(configuration, environment);
                })
                .ConfigureContainer<ContainerBuilder>((context, builder) =>
                {
                    // Any Autofac specific DI setup goes here, such as, adding Decorators.
                })
                .ConfigureAppConfiguration((context, builder) =>
                {
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        builder.AddUserSecrets<Program>();
                    }
                })
                .UseSerilog((context, services, configuration) =>
                {
                    configuration
                        .ReadFrom.Configuration(context.Configuration)
                        .ReadFrom.Services(services);
                });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/error-local-development");

                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DoctorDay.API v1"));
            }
            else
            {
                app.UseExceptionHandler("/error");
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
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

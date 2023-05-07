using BuildingBlocks.JsonConverters;

using DoctorDay.Application.Commands;
using DoctorDay.Application.Queries;
using DoctorDay.Domain.DayAggregate;
using DoctorDay.Infrastructure.RavenDB;

using Eventuous;
using Eventuous.EventStore;

using Microsoft.OpenApi.Models;

using Raven.Client.Documents;

namespace DoctorDay.API;

public class Startup
{
    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        Configuration = configuration;
        Environment = environment;
    }

    IConfiguration Configuration { get; }
    IWebHostEnvironment Environment { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        _ = services.AddControllers()
            .AddJsonOptions(opts => {
                opts.JsonSerializerOptions.Converters.Add(new TimeSpanConverter());
            });

        _ = services.AddSwaggerGen(c => {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "DoctorDay.API", Version = "v1" });
        });

        _ = Environment.IsDevelopment()
            ? services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace))
            : services.AddLogging();

        //Serialization
        _ = services.AddSingleton<System.Text.Json.JsonSerializerOptions>((provider) => {
            var options = new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new TimeSpanConverter());
            return options;
        });

        //Eventuous
        services.AddEventStoreClient(Configuration["EventStore:ConnectionString"]!)
                .AddAggregateStore<EsdbEventStore>()
                .AddSingleton<IEventSerializer, DefaultEventSerializer>();

        //Services
        services.AddCommandService<DayCommandService, Day>();
        DayEvents.MapDayEvents();

        //RavenDB
        _ = services.AddSingleton<IDocumentStore>((provider) => {
            var documentStore = new DocumentStore
            {
                Urls = Configuration["RavenDB:Server"].Split(',', System.StringSplitOptions.RemoveEmptyEntries),
                Database = Configuration["RavenDB:Database"],
                Conventions = {
                    AggressiveCache = {
                        Duration = TimeSpan.FromDays(1),
                        //Need to track, collections will be modified in the catch-up subscription
                        Mode = Raven.Client.Http.AggressiveCacheMode.TrackChanges
                    }
                }
            }.Initialize();

            documentStore.AggressivelyCache();

            return documentStore;
        })
        .AddSingleton<IAvailableSlotsRepository, RavenAvailableSlotsRepository>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
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
        app.UseRouting();
        app.UseAuthorization();

        app.UseEndpoints(endpoints => {
            endpoints.MapControllers();
        });
    }
}

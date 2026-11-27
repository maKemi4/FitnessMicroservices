using Microsoft.EntityFrameworkCore;
using Prometheus;
using Serilog;
using UserProfileService.Data;
using Serilog.Events;
using Serilog.Formatting.Json;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;


var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", builder.Environment.ApplicationName)
        .Enrich.WithProperty("Instance", Environment.MachineName)
        .WriteTo.Console(new JsonFormatter(renderMessage: true));
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: builder.Environment.ApplicationName,
            serviceInstanceId: Environment.MachineName))
    .WithTracing(tracing => tracing
        .SetSampler(new AlwaysOnSampler())
        .AddAspNetCoreInstrumentation(options =>
        {
            options.Filter = context =>
                !context.Request.Path.StartsWithSegments("/metrics");
        })
        .AddHttpClientInstrumentation()
        .AddSource("FitnessMicroservices")
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://jaeger:4317");
            options.Protocol = OtlpExportProtocol.Grpc;
        }));

// Підключення контролерів
builder.Services.AddControllers();

builder.Services.AddDbContext<ProfilesDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ProfilesDb")
        ?? throw new InvalidOperationException("Не задано підключення ProfilesDb.")));

// Створення застосунку
var app = builder.Build();

app.UseRouting();

app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/metrics"),
    branch =>
    {
        branch.UseHttpMetrics();
        branch.UseSerilogRequestLogging();
    });

// Маршрутизація HTTP-запитів
app.MapControllers();

// Перевірка роботи сервісу
app.MapGet("/", () =>
    Results.Ok(new
    {
        Service = "UserProfileService",
        Status = "Running"
    })
);

app.MapMetrics();

app.Run();
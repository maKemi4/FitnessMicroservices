using LoadCalculationService.Data;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using Serilog;
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

builder.Services.AddControllers();

builder.Services.AddDbContext<LoadDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("LoadDb")
        ?? throw new InvalidOperationException("Не задано підключення LoadDb.")));


// HTTP-клієнт для зв'язку з UserProfileService
builder.Services.AddHttpClient(
    "UserProfileService",
    client =>
    {
        var serviceUrl = builder.Configuration[
            "Services:UserProfileService"];

        if (string.IsNullOrWhiteSpace(serviceUrl))
        {
            throw new InvalidOperationException(
                "Не задано адресу UserProfileService");
        }

        client.BaseAddress = new Uri(serviceUrl);
        client.Timeout = TimeSpan.FromSeconds(10);
    });

var app = builder.Build();

app.UseRouting();

app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/metrics"),
    branch =>
    {
        branch.UseHttpMetrics();
        branch.UseSerilogRequestLogging();
    });


app.MapControllers();

app.MapGet("/", () =>
    Results.Ok(new
    {
        Service = "LoadCalculationService",
        Status = "Running"
    }));

app.MapMetrics();

app.Run();

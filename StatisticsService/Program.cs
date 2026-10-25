using StatisticsService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSingleton<StatisticsStore>();

builder.Services.AddHostedService<RabbitMqConsumer>();

var app = builder.Build();

app.MapControllers();

app.MapGet("/", () =>
    Results.Ok(new
    {
        Service = "StatisticsService",
        Status = "Running"
    }));

app.Run();
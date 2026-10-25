using TrainingService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSingleton<RabbitMqPublisher>();

var app = builder.Build();

app.MapControllers();

app.MapGet("/", () =>
    Results.Ok(new
    {
        Service = "TrainingService",
        Status = "Running"
    }));

app.Run();

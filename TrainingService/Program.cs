using TrainingService.Services;
using Microsoft.EntityFrameworkCore;
using TrainingService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<TrainingDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("TrainingDb")
        ?? throw new InvalidOperationException(
            "Connection string 'TrainingDb' is missing.")));

builder.Services.AddSingleton<RabbitMqPublisher>();

builder.Services.AddHostedService<OutboxPublisherService>();

builder.Services.AddHostedService<TrainingCompensationService>();

var app = builder.Build();

app.MapControllers();

app.MapGet("/", () =>
    Results.Ok(new
    {
        Service = "TrainingService",
        Status = "Running"
    }));

app.Run();

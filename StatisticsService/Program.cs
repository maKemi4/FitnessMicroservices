using StatisticsService.Services;
using Microsoft.EntityFrameworkCore;
using StatisticsService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<StatisticsDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("StatisticsDb")
        ?? throw new InvalidOperationException(
            "Connection string 'StatisticsDb' is missing."),
        postgres => postgres.MigrationsHistoryTable(
            "__EFMigrationsHistory", "statistics")));

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
using FitnessMcpServer.Services;
using FitnessMcpServer.Tools;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;
using FitnessMcpServer.Resources;
using FitnessMcpServer.Prompts;

var builder = WebApplication.CreateBuilder(args);

// Сервер доступний лише на цьому комп'ютері.
builder.WebHost.UseUrls("http://127.0.0.1:5050");
builder.Configuration["AllowedHosts"] = "localhost;127.0.0.1";

// Клієнт сервісу користувачів.
builder.Services.AddHttpClient<UserProfileApiClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:8080");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Клієнт сервісу статистики.
builder.Services.AddHttpClient<StatisticsApiClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:8080");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<TrainingApiClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:8080");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddTransient<TrainingTools>();

builder.Services.AddTransient<UserTools>();
builder.Services.AddTransient<StatisticsTools>();

builder.Services.AddHttpClient<LoadCalculationApiClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:8080");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddTransient<LoadTools>();

// Підключення MCP через Streamable HTTP.
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly()
    .WithResources<FitnessResources>()
    .WithPrompts<FitnessPrompts>();

var app = builder.Build();

// Звичайний HTTP-маршрут для перевірки запуску.
app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "FitnessMcpServer"
}));

// Адреса для MCP-клієнтів.
app.MapMcp("/mcp");

app.Run();
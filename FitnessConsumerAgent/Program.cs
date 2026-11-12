using A2A;
using A2A.AspNetCore;
using FitnessConsumerAgent.Agents;
using FitnessConsumerAgent.Services;
using FitnessConsumerAgent.Ai;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://127.0.0.1:5062");
builder.Configuration["AllowedHosts"] = "localhost;127.0.0.1";

builder.Services.AddHttpClient("FitnessApi", client =>
{
    client.BaseAddress = new Uri("http://localhost:8080");
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddSingleton<StatisticsVerificationService>();

builder.Services.AddHttpClient("Gemini", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddSingleton<StatisticsSummaryService>();

builder.Services.AddA2AAgent<ConsumerAgent>(
    ConsumerAgent.GetCard("http://127.0.0.1:5062/a2a"));

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "FitnessConsumerAgent"
}));

app.MapA2A("/a2a");

var card = app.Services.GetRequiredService<AgentCard>();
app.MapWellKnownAgentCard(card);

app.Run();
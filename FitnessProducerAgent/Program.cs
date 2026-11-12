using A2A;
using A2A.AspNetCore;
using FitnessProducerAgent.Agents;
using FitnessProducerAgent.Services;
using FitnessProducerAgent.Ai;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://127.0.0.1:5071");
builder.Configuration["AllowedHosts"] = "localhost;127.0.0.1";

builder.Services.AddHttpClient("FitnessApi", client =>
{
    client.BaseAddress = new Uri("http://localhost:8080");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient("ConsumerA2A", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddSingleton<TrainingCompletionService>();
builder.Services.AddSingleton<ConsumerAgentClient>();

builder.Services.AddHttpClient("Gemini", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddSingleton<TrainingRequestParser>();

builder.Services.AddA2AAgent<ProducerAgent>(
    ProducerAgent.GetCard("http://127.0.0.1:5071/a2a"));

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "FitnessProducerAgent"
}));

app.MapA2A("/a2a");

var card = app.Services.GetRequiredService<AgentCard>();
app.MapWellKnownAgentCard(card);

app.Run();
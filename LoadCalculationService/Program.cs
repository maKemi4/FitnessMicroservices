var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

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

app.MapControllers();

app.MapGet("/", () =>
    Results.Ok(new
    {
        Service = "LoadCalculationService",
        Status = "Running"
    }));

app.Run();

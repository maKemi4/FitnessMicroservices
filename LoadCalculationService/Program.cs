using LoadCalculationService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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

app.MapControllers();

app.MapGet("/", () =>
    Results.Ok(new
    {
        Service = "LoadCalculationService",
        Status = "Running"
    }));

app.Run();

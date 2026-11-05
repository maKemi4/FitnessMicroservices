using Microsoft.EntityFrameworkCore;
using UserProfileService.Data;

var builder = WebApplication.CreateBuilder(args);

// Підключення контролерів
builder.Services.AddControllers();

builder.Services.AddDbContext<ProfilesDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ProfilesDb")
        ?? throw new InvalidOperationException("Не задано підключення ProfilesDb.")));

// Створення застосунку
var app = builder.Build();

// Маршрутизація HTTP-запитів
app.MapControllers();

// Перевірка роботи сервісу
app.MapGet("/", () =>
    Results.Ok(new
    {
        Service = "UserProfileService",
        Status = "Running"
    })
);

app.Run();
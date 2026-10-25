var builder = WebApplication.CreateBuilder(args);

// Підключення контролерів
builder.Services.AddControllers();

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

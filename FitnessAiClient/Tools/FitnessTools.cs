using System.Text.Json;
using FitnessAiClient.Services;

namespace FitnessAiClient.Tools;

public sealed class FitnessTools(
    UserProfileApiClient users,
    StatisticsApiClient statistics)
{
    public JsonElement[] Definitions { get; } = CreateDefinitions();

    public Task<string> ExecuteAsync(
        string name,
        JsonElement arguments,
        CancellationToken cancellationToken = default)
    {
        if (arguments.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                "Аргументи інструмента мають бути об'єктом JSON.");
        }

        switch (name)
        {
            case "get_users":
                {
                    if (arguments.EnumerateObject().Any())
                    {
                        throw new InvalidOperationException(
                            "get_users не приймає аргументів.");
                    }

                    return users.GetUsersAsync(cancellationToken);
                }

            case "get_statistics":
                {
                    if (arguments.EnumerateObject().Count() != 1
                        || !arguments.TryGetProperty("userId", out var id)
                        || id.ValueKind != JsonValueKind.Number
                        || !id.TryGetInt32(out var userId)
                        || userId <= 0)
                    {
                        throw new InvalidOperationException(
                            "get_statistics потребує додатного цілого userId.");
                    }

                    return statistics.GetByUserAsync(
                        userId,
                        cancellationToken);
                }

            default:
                throw new InvalidOperationException(
                    $"Невідомий інструмент: {name}");
        }
    }

    private static JsonElement[] CreateDefinitions()
    {
        using var document = JsonDocument.Parse("""
            [
              {
                "type": "function",
                "name": "get_users",
                "description": "Отримує актуальний список користувачів та їхні ID. Використовуй для пошуку користувача за ім'ям.",
                "parameters": {
                  "type": "object",
                  "properties": {},
                  "required": []
                }
              },
              {
                "type": "function",
                "name": "get_statistics",
                "description": "Отримує статистику тренувань користувача за його ID. Якщо відоме лише ім'я, спочатку знайди ID через get_users.",
                "parameters": {
                  "type": "object",
                  "properties": {
                    "userId": {
                      "type": "integer",
                      "description": "Справжній ID користувача, отриманий із системи.",
                      "minimum": 1
                    }
                  },
                  "required": ["userId"]
                }
              }
            ]
            """);

        return document.RootElement
            .EnumerateArray()
            .Select(item => item.Clone())
            .ToArray();
    }
}
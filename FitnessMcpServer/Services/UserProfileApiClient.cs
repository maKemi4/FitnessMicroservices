using System.Text.Json;
using System.Net.Http.Json;
using FitnessMcpServer.Security;

namespace FitnessMcpServer.Services;

public sealed class UserProfileApiClient(HttpClient http)
{
    public async Task<string> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("[API] GET http://localhost:8080/api/users");

        using var response = await http.GetAsync("/api/users", cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"API користувачів: HTTP {(int)response.StatusCode}.");

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("API користувачів повернув не масив JSON.");

        Console.WriteLine($"[API] Отримано користувачів: {document.RootElement.GetArrayLength()}");
        return json;
    }

    public async Task<string> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        ValidateId(userId);

        using var response = await http.GetAsync(
            $"/api/users/{userId}",
            cancellationToken);

        return await ReadUserResponseAsync(response, cancellationToken);
    }

    public async Task<string> CreateAsync(string name, double height, double weight, CancellationToken cancellationToken = default)
    {
        ValidateProfile(name, height, weight);

        using var response = await http.PostAsJsonAsync(
            "/api/users",
            new { name, height, weight },
            cancellationToken);

        return await ReadUserResponseAsync(response, cancellationToken);
    }

    public async Task<string> UpdateAsync(int userId, string name, double height, double weight, CancellationToken cancellationToken = default)
    {
        ValidateId(userId);
        ValidateProfile(name, height, weight);

        using var response = await http.PutAsJsonAsync(
            $"/api/users/{userId}",
            new { name, height, weight },
            cancellationToken);

        return await ReadUserResponseAsync(response, cancellationToken);
    }

    public async Task<string> DeleteAsync(int userId, CancellationToken cancellationToken = default)
    {
        ValidateId(userId);

        using var response = await http.DeleteAsync(
            $"/api/users/{userId}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Видалення користувача: HTTP {(int)response.StatusCode}.");
        }

        return JsonSerializer.Serialize(new
        {
            deleted = true,
            userId
        });
    }

    private static void ValidateId(int userId)
    {
        if (userId <= 0)
            throw new ArgumentException("userId має бути додатним.");
    }

    private static void ValidateProfile(string name, double height, double weight)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
        {
            throw new ArgumentException(
                "Ім'я має містити від 1 до 100 символів.");
        }

        if (!double.IsFinite(height) || height <= 0
            || !double.IsFinite(weight) || weight <= 0)
        {
            throw new ArgumentException(
                "Зріст і вага мають бути додатними скінченними числами.");
        }

        // Перевірка до запису даних у базу.
        PromptInjectionGuard.CheckUserPrompt(name);
    }

    private static async Task<string> ReadUserResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"API користувачів: HTTP {(int)response.StatusCode}.");
        }

        var json = await response.Content.ReadAsStringAsync(
            cancellationToken);

        using var document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                "API користувачів повернув не об'єкт JSON.");
        }

        return json;
    }
}

using System.Text.Json;

namespace FitnessMcpServer.Services;

public sealed class StatisticsApiClient(HttpClient http)
{
    public async Task<string> GetByUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            throw new ArgumentException("userId має бути додатним.");

        Console.WriteLine(
            $"[API] GET http://localhost:8080/api/statistics/{userId}");

        using var response = await http.GetAsync(
            $"/api/statistics/{userId}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"API статистики: HTTP {(int)response.StatusCode}.");
        }

        var json = await response.Content.ReadAsStringAsync(
            cancellationToken);

        using var document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                "API статистики повернув не об'єкт JSON.");
        }

        Console.WriteLine("[API] Статистику отримано.");

        return json;
    }

    public async Task<string> GetAllAsync(
    CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync(
            "/api/statistics",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(
            cancellationToken);

        using var document = JsonDocument.Parse(json);

        return json;
    }
}
using System.Text.Json;

namespace FitnessMcpServer.Services;

public sealed class LoadCalculationApiClient(HttpClient http)
{
    public async Task<string> CalculateAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentException(
                "userId має бути додатним цілим числом.");
        }

        Console.WriteLine($"[API] GET /api/load/{userId}");

        using var response = await http.GetAsync(
            $"/api/load/{userId}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"API навантаження: HTTP {(int)response.StatusCode}.");
        }

        var json = await response.Content.ReadAsStringAsync(
            cancellationToken);

        using var document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                "API навантаження повернув не об'єкт JSON.");
        }

        Console.WriteLine("[API] Навантаження розраховано, план збережено.");

        return json;
    }
}
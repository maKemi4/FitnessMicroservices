using System.Text.Json;

namespace FitnessAiClient.Services;

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
}

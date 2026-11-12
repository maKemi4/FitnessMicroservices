using System.Text.Json;
using System.Net.Http.Json;
using FitnessMcpServer.Security;

namespace FitnessMcpServer.Services;

public sealed class TrainingApiClient(HttpClient http)
{
    public Task<string> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return GetAsync(
            "/api/trainings",
            JsonValueKind.Array,
            cancellationToken);
    }

    public Task<string> GetByIdAsync(
        int trainingId,
        CancellationToken cancellationToken = default)
    {
        if (trainingId <= 0)
        {
            throw new ArgumentException(
                "trainingId має бути додатним цілим числом.");
        }

        return GetAsync(
            $"/api/trainings/{trainingId}",
            JsonValueKind.Object,
            cancellationToken);
    }

    private async Task<string> GetAsync(
        string path,
        JsonValueKind expectedKind,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"[API] GET {path}");

        using var response = await http.GetAsync(
            path,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"API тренувань: HTTP {(int)response.StatusCode}.");
        }

        var json = await response.Content.ReadAsStringAsync(
            cancellationToken);

        using var document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind != expectedKind)
        {
            throw new InvalidOperationException(
                "API тренувань повернув неочікувану структуру JSON.");
        }

        return json;
    }

    public Task<string> StartAsync(
    int userId,
    CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            throw new ArgumentException("userId має бути додатним.");

        return PostAsync(
            "/api/trainings",
            new { userId },
            cancellationToken);
    }

    public Task<string> AddExerciseAsync(
        int trainingId,
        string name,
        int repetitions,
        int durationMinutes,
        CancellationToken cancellationToken = default)
    {
        if (trainingId <= 0)
            throw new ArgumentException("trainingId має бути додатним.");

        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            throw new ArgumentException("Назва вправи: від 1 до 100 символів.");

        if (repetitions < 0 || durationMinutes < 0)
        {
            throw new ArgumentException(
                "Кількість повторень і тривалість не можуть бути від'ємними.");
        }

        PromptInjectionGuard.CheckUserPrompt(name);

        return PostAsync(
            $"/api/trainings/{trainingId}/exercises",
            new { name, repetitions, durationMinutes },
            cancellationToken);
    }

    public Task<string> CompleteAsync(
        int trainingId,
        CancellationToken cancellationToken = default)
    {
        if (trainingId <= 0)
            throw new ArgumentException("trainingId має бути додатним.");

        return PostAsync(
            $"/api/trainings/{trainingId}/complete",
            null,
            cancellationToken);
    }

    private async Task<string> PostAsync(
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"[API] POST {path}");

        using var request = new HttpRequestMessage(HttpMethod.Post, path);

        if (body is not null)
            request.Content = JsonContent.Create(body);

        using var response = await http.SendAsync(
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"API тренувань: HTTP {(int)response.StatusCode}.");
        }

        var json = await response.Content.ReadAsStringAsync(
            cancellationToken);

        using var document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                "API тренувань повернув не об'єкт JSON.");
        }

        return json;
    }
}
using System.Text.Json;

namespace FitnessProducerAgent.Services;

public sealed class TrainingCompletionService(IHttpClientFactory factory)
{
    public async Task<int> CompleteAsync(
        int trainingId,
        CancellationToken cancellationToken)
    {
        if (trainingId <= 0)
            throw new ArgumentException("trainingId має бути додатним.");

        using var http = factory.CreateClient("FitnessApi");

        using var response = await http.GetAsync(
            $"/api/trainings/{trainingId}",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(
            cancellationToken);

        using var document = JsonDocument.Parse(json);
        var training = document.RootElement;

        var userId = training.GetProperty("userId").GetInt32();
        var status = training.GetProperty("status").GetString();

        if (status == "Completed")
        {
            Console.WriteLine(
                "[Producer] Тренування вже завершене. Перевіримо статистику.");

            return userId;
        }

        if (status != "InProgress")
            throw new InvalidOperationException("Неочікуваний статус тренування.");

        Console.WriteLine(
            $"[Producer] Завершуємо тренування {trainingId} через API.");

        using var completion = await http.PostAsync(
            $"/api/trainings/{trainingId}/complete",
            null,
            cancellationToken);

        completion.EnsureSuccessStatusCode();

        Console.WriteLine("[Producer] Тренування завершено, подія записана в Outbox.");

        return userId;
    }
}
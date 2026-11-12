using System.Text.Json;

namespace FitnessConsumerAgent.Services;

public sealed class StatisticsVerificationService(IHttpClientFactory factory)
{
    public async Task<string> VerifyAsync(
        int userId,
        int trainingId,
        CancellationToken cancellationToken)
    {
        if (userId <= 0 || trainingId <= 0)
        {
            throw new ArgumentException(
                "userId та trainingId мають бути додатними.");
        }

        using var client = factory.CreateClient("FitnessApi");

        using var deadline = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken);

        deadline.CancelAfter(TimeSpan.FromSeconds(20));

        try
        {
            while (true)
            {
                Console.WriteLine(
                    $"[Consumer] Перевіряємо тренування {trainingId} " +
                    $"у статистиці користувача {userId}.");

                using var response = await client.GetAsync(
                    $"/api/statistics/{userId}",
                    deadline.Token);

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(
                    deadline.Token);

                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                var found = root.GetProperty("trainingHistory")
                    .EnumerateArray()
                    .Any(item =>
                        item.GetProperty("trainingId").GetInt32() == trainingId);

                if (found)
                {
                    return JsonSerializer.Serialize(new
                    {
                        confirmed = true,
                        userId,
                        trainingId,
                        totalTrainings = root.GetProperty(
                            "totalTrainings").GetInt32(),
                        totalDurationMinutes = root.GetProperty(
                            "totalDurationMinutes").GetDouble(),
                        totalExercises = root.GetProperty(
                            "totalExercises").GetInt32()
                    });
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(1),
                    deadline.Token);
            }
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                "Не вдалося підтвердити появу тренування " +
                "у статистиці протягом 20 секунд.");
        }
    }
}
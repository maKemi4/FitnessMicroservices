using System.Text.Json;
using A2A;
using FitnessConsumerAgent.Services;
using FitnessConsumerAgent.Ai;

namespace FitnessConsumerAgent.Agents;

public sealed class ConsumerAgent(
    StatisticsVerificationService statistics,
    StatisticsSummaryService summaryService) : IAgentHandler
{
    public async Task ExecuteAsync(
        RequestContext context,
        AgentEventQueue eventQueue,
        CancellationToken cancellationToken)
    {
        var updater = new TaskUpdater(
            eventQueue,
            context.TaskId,
            context.ContextId);

        await updater.SubmitAsync(
            cancellationToken: cancellationToken);

        try
        {
            var text = context.UserText;

            if (string.IsNullOrWhiteSpace(text) || text.Length > 2000)
            {
                throw new ArgumentException(
                    "Очікується JSON-завдання довжиною до 2000 символів.");
            }

            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object
                || root.EnumerateObject().Count() != 2
                || !root.TryGetProperty("userId", out var userElement)
                || !root.TryGetProperty("trainingId", out var trainingElement)
                || userElement.ValueKind != JsonValueKind.Number
                || trainingElement.ValueKind != JsonValueKind.Number
                || !userElement.TryGetInt32(out var userId)
                || !trainingElement.TryGetInt32(out var trainingId)
                || userId <= 0
                || trainingId <= 0)
            {
                throw new ArgumentException(
                    "Потрібні лише додатні цілі userId та trainingId.");
            }

            await updater.StartWorkAsync(
                cancellationToken: cancellationToken);

            var result = await statistics.VerifyAsync(
                userId,
                trainingId,
                cancellationToken);

            var report = System.Text.Json.Nodes.JsonNode
                .Parse(result)!
                .AsObject();

            try
            {
                var summary = await summaryService.SummarizeAsync(
                    result,
                    cancellationToken);

                report["aiSummary"] = summary;
                report["aiSummaryStatus"] = "completed";
            }
            catch (Exception exception)
                when (exception is not OperationCanceledException
                      || !cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine(
                    $"[Consumer AI] Пояснення недоступне: {exception.Message}");

                report["aiSummary"] = null;
                report["aiSummaryStatus"] = "unavailable";
            }

            result = report.ToJsonString();

            await updater.AddArtifactAsync(
                [Part.FromText(result)],
                name: "statistics_confirmation",
                cancellationToken: cancellationToken);

            await updater.CompleteAsync(
                cancellationToken: cancellationToken);

            Console.WriteLine(
                $"[Consumer] Завдання {context.TaskId} завершено.");
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            Console.WriteLine(
                $"[Consumer] Помилка: {exception.Message}");

            await updater.FailAsync(
                message: new Message
                {
                    MessageId = Guid.NewGuid().ToString("N"),
                    Role = Role.Agent,
                    Parts =
                    [
                        Part.FromText(
                            $"Перевірка не виконана: {exception.Message}")
                    ]
                },
                cancellationToken: cancellationToken);
        }
    }

    public static AgentCard GetCard(string endpoint)
    {
        return new AgentCard
        {
            Name = "Fitness Consumer Agent",
            Description =
                "Перевіряє появу завершеного тренування " +
                "у StatisticsService після обробки події RabbitMQ.",
            Version = "1.0.0",

            SupportedInterfaces =
            [
                new AgentInterface
                {
                    Url = endpoint,
                    ProtocolBinding = "JSONRPC",
                    ProtocolVersion = "1.0"
                }
            ],

            DefaultInputModes = ["text/plain"],
            DefaultOutputModes = ["text/plain"],

            Capabilities = new AgentCapabilities
            {
                Streaming = false,
                PushNotifications = false
            },

            Skills =
            [
                new AgentSkill
                {
                    Id = "verify_training_statistics",
                    Name = "Verify training statistics",
                    Description =
                        "Приймає JSON-текст із userId та trainingId. " +
                        "До 20 секунд очікує появу тренування у статистиці.",
                    Tags = ["fitness", "statistics", "consumer"],
                    Examples =
                    [
                        """{"userId":1,"trainingId":8}"""
                    ]
                }
            ]
        };
    }
}
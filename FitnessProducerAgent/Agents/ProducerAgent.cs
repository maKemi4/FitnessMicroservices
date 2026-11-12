using System.Text.Json;
using A2A;
using FitnessProducerAgent.Services;
using FitnessProducerAgent.Ai;

namespace FitnessProducerAgent.Agents;

public sealed class ProducerAgent(
    TrainingCompletionService trainings,
    ConsumerAgentClient consumer,
    TrainingRequestParser parser) : IAgentHandler
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

            var trainingId = await parser.ParseAsync(
                context.UserText,
                cancellationToken);

            await updater.StartWorkAsync(
                cancellationToken: cancellationToken);

            var userId = await trainings.CompleteAsync(
                trainingId,
                cancellationToken);

            Console.WriteLine(
                "[Producer] Передаємо consumer A2A-завдання перевірити статистику.");

            var result = await consumer.VerifyAsync(
                userId,
                trainingId,
                cancellationToken);

            await updater.AddArtifactAsync(
                [Part.FromText(result)],
                name: "training_completion_result",
                cancellationToken: cancellationToken);

            await updater.CompleteAsync(
                cancellationToken: cancellationToken);

            Console.WriteLine(
                $"[Producer] Завдання {context.TaskId} завершено.");
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            Console.WriteLine(
                $"[Producer] Помилка: {exception.Message}");

            await updater.FailAsync(
                message: new Message
                {
                    MessageId = Guid.NewGuid().ToString("N"),
                    Role = Role.Agent,
                    Parts = [Part.FromText(exception.Message)]
                },
                cancellationToken: cancellationToken);
        }
    }

    public static AgentCard GetCard(string endpoint)
    {
        return new AgentCard
        {
            Name = "Fitness Producer Agent",
            Description =
                "Приймає текстовий запит завершити тренування з конкретним ID " +
                "або JSON із trainingId. Використовує Gemini для розуміння тексту, " +
                "завершує тренування й отримує A2A-підтвердження від consumer.",
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
                    Id = "complete_training_and_verify",
                    Name = "Complete training and verify",
                    Description =
                        "Приймає JSON із trainingId. Завершує тренування " +
                        "та отримує A2A-підтвердження від consumer.",
                    Tags = ["fitness", "training", "producer"],
                    Examples =
                    [
                        "Заверши тренування №10 і перевір статистику.",
                        """{"trainingId":10}"""
                    ]
                }
            ]
        };
    }
}
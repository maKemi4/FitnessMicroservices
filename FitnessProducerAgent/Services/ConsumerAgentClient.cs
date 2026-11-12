using System.Text.Json;
using A2A;

namespace FitnessProducerAgent.Services;

public sealed class ConsumerAgentClient(IHttpClientFactory factory)
{
    public async Task<string> VerifyAsync(
        int userId,
        int trainingId,
        CancellationToken cancellationToken)
    {
        using var http = factory.CreateClient("ConsumerA2A");

        var resolver = new A2ACardResolver(
            new Uri("http://127.0.0.1:5062/"),
            httpClient: http);

        var card = await resolver.GetAgentCardAsync(cancellationToken);

        if (!card.Skills.Any(skill =>
            skill.Id == "verify_training_statistics"))
        {
            throw new InvalidOperationException(
                "Consumer не оголосив потрібну навичку.");
        }

        var agentInterface = card.SupportedInterfaces.FirstOrDefault(
            item => item.ProtocolBinding == "JSONRPC");

        if (agentInterface is null
            || agentInterface.Url != "http://127.0.0.1:5062/a2a")
        {
            throw new InvalidOperationException(
                "Неочікувана адреса consumer у його Agent Card.");
        }

        Console.WriteLine(
            $"[A2A] Знайдено агента: {card.Name}");

        using var client = new A2AClient(
            new Uri(agentInterface.Url),
            http);

        var result = await client.SendMessageAsync(
            new SendMessageRequest
            {
                Message = new Message
                {
                    MessageId = Guid.NewGuid().ToString("N"),
                    Role = Role.User,
                    Parts =
                    [
                        Part.FromText(JsonSerializer.Serialize(new
                        {
                            userId,
                            trainingId
                        }))
                    ]
                },
                Configuration = new SendMessageConfiguration
                {
                    ReturnImmediately = false
                }
            },
            cancellationToken);

        var task = result.Task;

        if (task is null)
            throw new InvalidOperationException("Consumer не повернув A2A-завдання.");

        Console.WriteLine(
            $"[A2A] Завдання consumer: {task.Id}; стан: {task.Status.State}");

        if (task.Status.State != TaskState.Completed)
        {
            throw new InvalidOperationException(
                $"Consumer не підтвердив статистику. Стан: {task.Status.State}.");
        }

        var confirmationJson = task.Artifacts?
            .Where(artifact => artifact.Name == "statistics_confirmation")
            .SelectMany(artifact => artifact.Parts)
            .Select(part => part.Text)
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

        if (confirmationJson is null)
        {
            throw new InvalidOperationException(
                "Consumer не повернув artifact із підтвердженням.");
        }

        using var document = JsonDocument.Parse(confirmationJson);
        var confirmation = document.RootElement;

        if (!confirmation.GetProperty("confirmed").GetBoolean()
            || confirmation.GetProperty("userId").GetInt32() != userId
            || confirmation.GetProperty("trainingId").GetInt32() != trainingId)
        {
            throw new InvalidOperationException(
                "Підтвердження consumer не відповідає нашому завданню.");
        }

        return JsonSerializer.Serialize(new
        {
            consumerTaskId = task.Id,
            confirmation
        });
    }
}
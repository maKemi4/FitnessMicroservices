using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using TrainingService.Data;
using TrainingService.Models;

namespace TrainingService.Services;

public sealed class TrainingCompensationService(
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    ILogger<TrainingCompensationService> logger) : BackgroundService
{
    private const string QueueName = "training_completed.dlq";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = configuration["RabbitMQ:UserName"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest"
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection =
                    await factory.CreateConnectionAsync(stoppingToken);
                await using var channel =
                    await connection.CreateChannelAsync(
                        cancellationToken: stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var delivery = await channel.BasicGetAsync(
                        QueueName, autoAck: false, cancellationToken: stoppingToken);

                    if (delivery is null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                        continue;
                    }

                    TrainingCompletedEvent? failedEvent;

                    try
                    {
                        failedEvent = JsonSerializer
                            .Deserialize<TrainingCompletedEvent>(delivery.Body.Span);
                    }
                    catch (JsonException ex)
                    {
                        logger.LogWarning(ex, "Некоректний JSON у DLQ.");
                        await channel.BasicRejectAsync(
                            delivery.DeliveryTag, requeue: false,
                            cancellationToken: stoppingToken);
                        continue;
                    }

                    if (failedEvent is null ||
                        failedEvent.EventId == Guid.Empty ||
                        failedEvent.TrainingId <= 0 ||
                        failedEvent.CompletedAt.Kind != DateTimeKind.Utc)
                    {
                        logger.LogWarning("Некоректна подія завершення в DLQ.");
                        await channel.BasicRejectAsync(
                            delivery.DeliveryTag, requeue: false,
                            cancellationToken: stoppingToken);
                        continue;
                    }

                    await CompensateAsync(failedEvent, stoppingToken);

                    await channel.BasicAckAsync(
                        delivery.DeliveryTag, multiple: false,
                        cancellationToken: stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Помилка обробки DLQ. Повторимо спробу.");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }

    private async Task CompensateAsync(
        TrainingCompletedEvent failedEvent,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TrainingDbContext>();

        var changedRows = await db.Trainings
            .Where(training =>
                training.Id == failedEvent.TrainingId &&
                training.UserId == failedEvent.UserId &&
                training.Status == "Completed" &&
                training.EndTime == failedEvent.CompletedAt)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(training => training.Status, "InProgress")
                    .SetProperty(training => training.EndTime, (DateTime?)null),
                cancellationToken);

        if (changedRows == 1)
        {
            logger.LogInformation(
                "Saga: завершення тренування {TrainingId} відкочено.",
                failedEvent.TrainingId);
        }
        else
        {
            logger.LogInformation(
                "Saga: подія {EventId} не відповідає поточному завершенню тренування.",
                failedEvent.EventId);
        }
    }
}
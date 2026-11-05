using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TrainingService.Data;
using TrainingService.Models;

namespace TrainingService.Services;

public class OutboxPublisherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqPublisher _publisher;
    private readonly ILogger<OutboxPublisherService> _logger;

    public OutboxPublisherService(
        IServiceScopeFactory scopeFactory,
        RabbitMqPublisher publisher,
        ILogger<OutboxPublisherService> logger)
    {
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    ex, "Не вдалося відправити повідомлення з Outbox. Повторимо спробу.");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }

    private async Task PublishPendingMessagesAsync(CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TrainingDbContext>();

        var messages = await db.OutboxMessages
            .Where(message => message.PublishedAt == null)
            .OrderBy(message => message.CreatedAt)
            .Take(20)
            .ToListAsync(stoppingToken);

        foreach (var message in messages)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var trainingEvent =
                JsonSerializer.Deserialize<TrainingCompletedEvent>(message.Payload)
                ?? throw new InvalidOperationException(
                    $"Не вдалося прочитати подію {message.EventId} з Outbox.");

            await _publisher.PublishTrainingCompletedAsync(trainingEvent);

            message.PublishedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(stoppingToken);
        }
    }
}
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StatisticsService.Models;
using System.Text;
using System.Text.Json;

namespace StatisticsService.Services
{
    public class RabbitMqConsumer : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly StatisticsStore _store;
        private readonly ILogger<RabbitMqConsumer> _logger;

        public RabbitMqConsumer(
            IConfiguration configuration,
            StatisticsStore store,
            ILogger<RabbitMqConsumer> logger)
        {
            _configuration = configuration;
            _store = store;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ConsumeAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Помилка підключення до RabbitMQ");

                    try
                    {
                        await Task.Delay(
                            TimeSpan.FromSeconds(5),
                            stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private async Task ConsumeAsync(
            CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration[
                    "RabbitMQ:HostName"] ?? "localhost",

                Port = int.Parse(
                    _configuration["RabbitMQ:Port"] ?? "5672"),

                UserName = _configuration[
                    "RabbitMQ:UserName"] ?? "guest",

                Password = _configuration[
                    "RabbitMQ:Password"] ?? "guest"
            };

            await using var connection =
                await factory.CreateConnectionAsync(
                    cancellationToken: stoppingToken);

            await using var channel =
                await connection.CreateChannelAsync(
                    cancellationToken: stoppingToken);

            await channel.QueueDeclareAsync(
                queue: "training_completed",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            await channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: 1,
                global: false,
                cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (sender, args) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(
                        args.Body.ToArray());

                    var trainingEvent =
                        JsonSerializer.Deserialize<TrainingCompletedEvent>(
                            json,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                    if (trainingEvent == null ||
                        trainingEvent.EventId == Guid.Empty ||
                        trainingEvent.TrainingId <= 0 ||
                        trainingEvent.UserId <= 0)
                    {
                        _logger.LogWarning(
                            "Отримано некоректну подію");

                        await channel.BasicRejectAsync(
                            args.DeliveryTag,
                            requeue: false);

                        return;
                    }

                    bool added = _store.AddTraining(trainingEvent);

                    if (added)
                    {
                        _logger.LogInformation(
                            "TrainingCompleted отримано. " +
                            "TrainingId={TrainingId}, UserId={UserId}",
                            trainingEvent.TrainingId,
                            trainingEvent.UserId);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Повторну подію {EventId} пропущено",
                            trainingEvent.EventId);
                    }

                    await channel.BasicAckAsync(
                        args.DeliveryTag,
                        multiple: false);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(
                        ex,
                        "Некоректний JSON повідомлення");

                    await channel.BasicRejectAsync(
                        args.DeliveryTag,
                        requeue: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Помилка обробки повідомлення");

                    await channel.BasicNackAsync(
                        args.DeliveryTag,
                        multiple: false,
                        requeue: true);
                }
            };

            await channel.BasicConsumeAsync(
                queue: "training_completed",
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation(
                "RabbitMQ Consumer запущено. Очікування повідомлень...");

            await Task.Delay(
                Timeout.InfiniteTimeSpan,
                stoppingToken);
        }
    }
}
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using TrainingService.Models;
using System.Diagnostics;

namespace TrainingService.Services
{
    public class RabbitMqPublisher
    {
        private readonly IConfiguration _configuration;

        public RabbitMqPublisher(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private static readonly ActivitySource TraceSource = new("FitnessMicroservices");

        public async Task PublishTrainingCompletedAsync(
            TrainingCompletedEvent trainingEvent)
        {
            ActivityContext.TryParse(
                trainingEvent.TraceParent,
                null,
                false,
                out var parentContext);

            using var activity = TraceSource.StartActivity(
                "training_completed publish",
                ActivityKind.Producer,
                parentContext);

            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination.name", "training_completed");
            activity?.SetTag("messaging.message.id", trainingEvent.EventId.ToString());
            activity?.SetTag("training.id", trainingEvent.TrainingId);

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
                await factory.CreateConnectionAsync();

            var channelOptions = new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true,
                outstandingPublisherConfirmationsRateLimiter: null);

            await using var channel =
                await connection.CreateChannelAsync(channelOptions);

            await channel.QueueDeclareAsync(
                queue: "training_completed",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            string json = JsonSerializer.Serialize(trainingEvent);

            byte[] body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = trainingEvent.EventId.ToString()
            };

            var traceParent = activity?.Id ?? trainingEvent.TraceParent;

            if (traceParent is not null)
            {
                properties.Headers = new Dictionary<string, object?>
                {
                    ["traceparent"] = traceParent
                };
            }

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: "training_completed",
                mandatory: true,
                basicProperties: properties,
                body: body
            );

            Console.WriteLine(
                $"[RabbitMQ] TrainingCompleted: " +
                $"TrainingId={trainingEvent.TrainingId}");
        }
    }
}

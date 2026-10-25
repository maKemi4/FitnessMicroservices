using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using TrainingService.Models;

namespace TrainingService.Services
{
    public class RabbitMqPublisher
    {
        private readonly IConfiguration _configuration;

        public RabbitMqPublisher(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task PublishTrainingCompletedAsync(
            TrainingCompletedEvent trainingEvent)
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
                await factory.CreateConnectionAsync();

            await using var channel =
                await connection.CreateChannelAsync();

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

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: "training_completed",
                mandatory: false,
                basicProperties: properties,
                body: body
            );

            Console.WriteLine(
                $"[RabbitMQ] TrainingCompleted: " +
                $"TrainingId={trainingEvent.TrainingId}");
        }
    }
}

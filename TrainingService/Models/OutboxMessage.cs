namespace TrainingService.Models
{
    public class OutboxMessage
    {
        public Guid EventId { get; set; }
        public string Payload { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}

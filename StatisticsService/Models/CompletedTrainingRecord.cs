namespace StatisticsService.Models
{
    public class CompletedTrainingRecord
    {
        public Guid EventId { get; set; }
        public int UserId { get; set; }
        public string Payload { get; set; } = string.Empty;
    }
}

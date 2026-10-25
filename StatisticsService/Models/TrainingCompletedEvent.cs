namespace StatisticsService.Models
{
    public class TrainingCompletedEvent
    {
        public Guid EventId { get; set; }
        public int TrainingId { get; set; }
        public int UserId { get; set; }
        public DateTime CompletedAt { get; set; }
        public double DurationMinutes { get; set; }
        public List<Exercise> Exercises { get; set; } = new();
    }
}
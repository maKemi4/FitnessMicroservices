namespace StatisticsService.Models
{
    public class Exercise
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Repetitions { get; set; }
        public int DurationMinutes { get; set; }
    }
}
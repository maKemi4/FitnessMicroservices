namespace TrainingService.Models
{
    public class Training
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public string Status { get; set; } = "InProgress";

        public List<Exercise> Exercises { get; set; } = new();
    }
}

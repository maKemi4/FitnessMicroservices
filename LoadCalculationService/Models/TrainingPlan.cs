namespace LoadCalculationService.Models
{
    public class TrainingPlan
    {
        public int UserId { get; set; }
        public double BMI { get; set; }
        public string LoadLevel { get; set; } = string.Empty;
        public int RecommendedMinutes { get; set; }
        public string Recommendation { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}

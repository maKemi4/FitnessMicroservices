using Microsoft.AspNetCore.Mvc;
using StatisticsService.Services;

namespace StatisticsService.Controllers
{
    [ApiController]
    [Route("api/statistics")]
    public class StatisticsController : ControllerBase
    {
        private readonly StatisticsStore _store;

        public StatisticsController(StatisticsStore store)
        {
            _store = store;
        }

        // GET: api/statistics
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_store.GetAll());
        }

        // GET: api/statistics/1
        [HttpGet("{userId:int}")]
        public IActionResult GetByUser(int userId)
        {
            var trainings = _store.GetByUser(userId);

            var result = new
            {
                UserId = userId,
                TotalTrainings = trainings.Count,

                TotalDurationMinutes = Math.Round(
                    trainings.Sum(t => t.DurationMinutes), 2),

                TotalExercises = trainings.Sum(
                    t => t.Exercises.Count),

                TrainingHistory = trainings
                    .OrderByDescending(t => t.CompletedAt)
                    .ToList()
            };

            return Ok(result);
        }
    }
}
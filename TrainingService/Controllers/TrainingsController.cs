using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using TrainingService.Models;
using TrainingService.Services;

namespace TrainingService.Controllers
{
    [ApiController]
    [Route("api/trainings")]
    public class TrainingsController : ControllerBase
    {
        private static readonly ConcurrentDictionary<int, Training>
            Trainings = new();

        private static readonly SemaphoreSlim TrainingLock = new(1, 1);

        private static int _nextId = 0;
        private static int _nextExerciseId = 0;

        private readonly RabbitMqPublisher _publisher;

        public TrainingsController(RabbitMqPublisher publisher)
        {
            _publisher = publisher;
        }

        // GET: api/trainings
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(Trainings.Values.OrderBy(t => t.Id));
        }

        // GET: api/trainings/1
        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            if (!Trainings.TryGetValue(id, out var training))
                return NotFound("Тренування не знайдено");

            return Ok(training);
        }

        // POST: api/trainings
        [HttpPost]
        public IActionResult Start([FromBody] StartTrainingRequest request)
        {
            if (request.UserId <= 0)
                return BadRequest("Некоректний UserId");

            var training = new Training
            {
                Id = Interlocked.Increment(ref _nextId),
                UserId = request.UserId,
                StartTime = DateTime.UtcNow,
                Status = "InProgress"
            };

            Trainings[training.Id] = training;

            return CreatedAtAction(
                nameof(GetById),
                new { id = training.Id },
                training);
        }

        // POST: api/trainings/1/exercises
        [HttpPost("{id:int}/exercises")]
        public async Task<IActionResult> AddExercise(
            int id,
            [FromBody] Exercise exercise)
        {
            if (string.IsNullOrWhiteSpace(exercise.Name) ||
                exercise.Repetitions < 0 ||
                exercise.DurationMinutes < 0)
            {
                return BadRequest("Некоректні дані вправи");
            }

            await TrainingLock.WaitAsync();

            try
            {
                if (!Trainings.TryGetValue(id, out var training))
                    return NotFound("Тренування не знайдено");

                if (training.Status != "InProgress")
                    return BadRequest("Тренування вже завершено");

                var newExercise = new Exercise
                {
                    Id = Interlocked.Increment(
                        ref _nextExerciseId),
                    Name = exercise.Name,
                    Repetitions = exercise.Repetitions,
                    DurationMinutes = exercise.DurationMinutes
                };

                training.Exercises.Add(newExercise);

                return Ok(newExercise);
            }
            finally
            {
                TrainingLock.Release();
            }
        }

        // POST: api/trainings/1/complete
        [HttpPost("{id:int}/complete")]
        public async Task<IActionResult> Complete(int id)
        {
            await TrainingLock.WaitAsync();

            try
            {
                if (!Trainings.TryGetValue(id, out var training))
                    return NotFound("Тренування не знайдено");

                if (training.Status != "InProgress")
                    return BadRequest("Тренування вже завершено");

                var completedAt = DateTime.UtcNow;

                var trainingEvent = new TrainingCompletedEvent
                {
                    TrainingId = training.Id,
                    UserId = training.UserId,
                    CompletedAt = completedAt,
                    DurationMinutes = Math.Round(
                        (completedAt - training.StartTime)
                        .TotalMinutes, 2),
                    Exercises = training.Exercises
                        .Select(e => new Exercise
                        {
                            Id = e.Id,
                            Name = e.Name,
                            Repetitions = e.Repetitions,
                            DurationMinutes = e.DurationMinutes
                        }).ToList()
                };

                try
                {
                    await _publisher.PublishTrainingCompletedAsync(
                        trainingEvent);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"[RabbitMQ ERROR] {ex.Message}");

                    return StatusCode(
                        503,
                        "Не вдалося надіслати подію до RabbitMQ");
                }

                training.EndTime = completedAt;
                training.Status = "Completed";

                return Ok(new
                {
                    Message = "Тренування завершено",
                    Training = training,
                    EventPublished = true
                });
            }
            finally
            {
                TrainingLock.Release();
            }
        }
    }

    public class StartTrainingRequest
    {
        public int UserId { get; set; }
    }
}
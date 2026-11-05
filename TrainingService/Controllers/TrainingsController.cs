using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrainingService.Data;
using TrainingService.Models;
using TrainingService.Services;
using System.Text.Json;

namespace TrainingService.Controllers;

[ApiController]
[Route("api/trainings")]
public class TrainingsController : ControllerBase
{
    private static readonly SemaphoreSlim TrainingLock = new(1, 1);

    private readonly TrainingDbContext _db;
    private readonly RabbitMqPublisher _publisher;

    public TrainingsController(TrainingDbContext db)
    {
        _db = db;
    }

    // GET: api/trainings
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var trainings = await _db.Trainings
            .AsNoTracking()
            .Include(training => training.Exercises.OrderBy(exercise => exercise.Id))
            .OrderBy(training => training.Id)
            .ToListAsync();

        return Ok(trainings);
    }

    // GET: api/trainings/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var training = await _db.Trainings
            .AsNoTracking()
            .Include(item => item.Exercises.OrderBy(exercise => exercise.Id))
            .SingleOrDefaultAsync(item => item.Id == id);

        if (training is null)
            return NotFound("Тренування не знайдено");

        return Ok(training);
    }

    // POST: api/trainings
    [HttpPost]
    public async Task<IActionResult> Start([FromBody] StartTrainingRequest request)
    {
        if (request.UserId <= 0)
            return BadRequest("Некоректний UserId");

        var training = new Training
        {
            UserId = request.UserId,
            StartTime = DateTime.UtcNow,
            Status = "InProgress"
        };

        _db.Trainings.Add(training);
        await _db.SaveChangesAsync();

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
            var training = await _db.Trainings
                .SingleOrDefaultAsync(item => item.Id == id);

            if (training is null)
                return NotFound("Тренування не знайдено");

            if (training.Status != "InProgress")
                return BadRequest("Тренування вже завершено");

            var newExercise = new Exercise
            {
                Name = exercise.Name,
                Repetitions = exercise.Repetitions,
                DurationMinutes = exercise.DurationMinutes
            };

            training.Exercises.Add(newExercise);
            await _db.SaveChangesAsync();

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
            var training = await _db.Trainings
                .Include(item => item.Exercises.OrderBy(exercise => exercise.Id))
                .SingleOrDefaultAsync(item => item.Id == id);

            if (training is null)
                return NotFound("Тренування не знайдено");

            if (training.Status != "InProgress")
                return BadRequest("Тренування вже завершено");

            var completedAt = DateTime.UtcNow;

            var trainingEvent = new TrainingCompletedEvent
            {
                EventId = Guid.NewGuid(),
                TrainingId = training.Id,
                UserId = training.UserId,
                CompletedAt = completedAt,
                DurationMinutes = Math.Round(
                    (completedAt - training.StartTime).TotalMinutes, 2),
                Exercises = training.Exercises
                    .Select(exercise => new Exercise
                    {
                        Id = exercise.Id,
                        Name = exercise.Name,
                        Repetitions = exercise.Repetitions,
                        DurationMinutes = exercise.DurationMinutes
                    })
                    .ToList()
            };

            training.EndTime = completedAt;
            training.Status = "Completed";

            _db.OutboxMessages.Add(new OutboxMessage
            {
                EventId = trainingEvent.EventId,
                Payload = JsonSerializer.Serialize(trainingEvent),
                CreatedAt = completedAt
            });

            await _db.SaveChangesAsync();

            return Ok(new
            {
                Message = "Тренування завершено. Подія очікує відправки.",
                Training = training,
                EventQueued = true
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
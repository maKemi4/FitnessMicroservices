using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StatisticsService.Data;
using StatisticsService.Models;

namespace StatisticsService.Services;

public class StatisticsStore
{
    private readonly IServiceScopeFactory _scopeFactory;

    public StatisticsStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public bool AddTraining(TrainingCompletedEvent training)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StatisticsDbContext>();

        var payload = JsonSerializer.Serialize(training);

        var insertedRows = db.Database.ExecuteSqlInterpolated($"""
            INSERT INTO statistics.completed_trainings ("EventId", "UserId", "Payload")
            VALUES ({training.EventId}, {training.UserId}, CAST({payload} AS jsonb))
            ON CONFLICT ("EventId") DO NOTHING;
            """);

        return insertedRows == 1;
    }

    public List<TrainingCompletedEvent> GetAll()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StatisticsDbContext>();

        var payloads = db.CompletedTrainings
            .AsNoTracking()
            .Select(record => record.Payload)
            .ToList();

        return payloads.Select(DeserializeEvent).ToList();
    }

    public List<TrainingCompletedEvent> GetByUser(int userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StatisticsDbContext>();

        var payloads = db.CompletedTrainings
            .AsNoTracking()
            .Where(record => record.UserId == userId)
            .Select(record => record.Payload)
            .ToList();

        return payloads.Select(DeserializeEvent).ToList();
    }

    private static TrainingCompletedEvent DeserializeEvent(string payload)
    {
        return JsonSerializer.Deserialize<TrainingCompletedEvent>(payload)
            ?? throw new InvalidOperationException(
                "Не вдалося прочитати збережену подію тренування.");
    }
}
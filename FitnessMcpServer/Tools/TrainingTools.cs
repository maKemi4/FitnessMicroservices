using System.ComponentModel;
using FitnessMcpServer.Security;
using FitnessMcpServer.Services;
using ModelContextProtocol.Server;

namespace FitnessMcpServer.Tools;

[McpServerToolType]
public sealed class TrainingTools(TrainingApiClient trainings, UserProfileApiClient users)
{
    [McpServerTool(Name = "get_trainings")]
    [Description(
        "Отримує список усіх тренувань FitnessMicroservices " +
        "разом зі статусами та вправами.")]
    public async Task<string> GetTrainings(
        CancellationToken cancellationToken)
    {
        var json = await trainings.GetAllAsync(cancellationToken);

        return ApiResultGuard.Validate(json);
    }

    [McpServerTool(Name = "get_training")]
    [Description(
        "Отримує конкретне тренування за його ID, " +
        "включно зі статусом та списком вправ.")]
    public async Task<string> GetTraining(
        [Description("Додатний цілий ID тренування.")]
        int trainingId,
        CancellationToken cancellationToken)
    {
        if (trainingId <= 0)
        {
            throw new ArgumentException(
                "trainingId має бути додатним цілим числом.");
        }

        var json = await trainings.GetByIdAsync(
            trainingId,
            cancellationToken);

        return ApiResultGuard.Validate(json);
    }

    [McpServerTool(Name = "start_training")]
    [Description(
    "Починає нове тренування для наявного користувача. " +
    "Створює запис у базі. Використовуй за явним запитом почати тренування.")]
    public async Task<string> StartTraining(
    [Description("ID наявного користувача.")]
    int userId,
    CancellationToken cancellationToken)
    {
        // Якщо користувача немає, створення тренування не виконується.
        var userJson = await users.GetByIdAsync(
            userId,
            cancellationToken);

        ApiResultGuard.Validate(userJson);

        var json = await trainings.StartAsync(
            userId,
            cancellationToken);

        return ApiResultGuard.Validate(json);
    }

    [McpServerTool(Name = "add_exercise")]
    [Description(
        "Додає вправу до незавершеного тренування. " +
        "Змінює дані в базі. Використовуй за явним запитом додати вправу.")]
    public async Task<string> AddExercise(
        [Description("ID незавершеного тренування.")]
    int trainingId,
        [Description("Назва вправи.")]
    string name,
        [Description("Кількість повторень, ціле число від нуля.")]
    int repetitions,
        [Description("Тривалість вправи у хвилинах, ціле число від нуля.")]
    int durationMinutes,
        CancellationToken cancellationToken)
    {
        var json = await trainings.AddExerciseAsync(
            trainingId,
            name,
            repetitions,
            durationMinutes,
            cancellationToken);

        return ApiResultGuard.Validate(json);
    }

    [McpServerTool(Name = "complete_training")]
    [Description(
        "Завершує тренування та ставить подію в Transactional Outbox " +
        "для передавання через RabbitMQ до сервісу статистики. " +
        "Статистика оновлюється асинхронно. " +
        "Використовуй за явним запитом завершити конкретне тренування.")]
    public async Task<string> CompleteTraining(
        [Description("ID тренування, яке потрібно завершити.")]
    int trainingId,
        CancellationToken cancellationToken)
    {
        var json = await trainings.CompleteAsync(
            trainingId,
            cancellationToken);

        return ApiResultGuard.Validate(json);
    }
}
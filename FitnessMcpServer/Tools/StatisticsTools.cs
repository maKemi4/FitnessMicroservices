using System.ComponentModel;
using FitnessMcpServer.Security;
using FitnessMcpServer.Services;
using ModelContextProtocol.Server;

namespace FitnessMcpServer.Tools;

[McpServerToolType]
public sealed class StatisticsTools(StatisticsApiClient statistics)
{
    [McpServerTool(Name = "get_statistics")]
    [Description(
        "Отримує статистику тренувань користувача за його ID. " +
        "Якщо відоме лише ім'я, спочатку отримай ID через get_users.")]
    public async Task<string> GetStatistics(
        [Description("Додатний цілий ID користувача.")]
        int userId,
        CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            throw new ArgumentException(
                "userId має бути додатним цілим числом.");
        }

        var json = await statistics.GetByUserAsync(
            userId,
            cancellationToken);

        return ApiResultGuard.Validate(json);
    }

    [McpServerTool(Name = "get_all_statistics")]
    [Description("Отримує всі записи статистики завершених тренувань.")]
    public async Task<string> GetAllStatistics(
    CancellationToken cancellationToken)
    {
        var json = await statistics.GetAllAsync(cancellationToken);

        return ApiResultGuard.Validate(json);
    }
}
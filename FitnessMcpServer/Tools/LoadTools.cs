using System.ComponentModel;
using FitnessMcpServer.Security;
using FitnessMcpServer.Services;
using ModelContextProtocol.Server;

namespace FitnessMcpServer.Tools;

[McpServerToolType]
public sealed class LoadTools(LoadCalculationApiClient load)
{
    [McpServerTool(Name = "calculate_load")]
    [Description(
        "Розраховує демонстраційне фізичне навантаження " +
        "за зростом і вагою користувача та зберігає новий план. " +
        "Кожен успішний виклик створює запис плану. " +
        "Використовуй, коли користувач просить розрахувати навантаження.")]
    public async Task<string> CalculateLoad(
        [Description("Додатний цілий ID користувача.")]
        int userId,
        CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            throw new ArgumentException(
                "userId має бути додатним цілим числом.");
        }

        var json = await load.CalculateAsync(
            userId,
            cancellationToken);

        return ApiResultGuard.Validate(json);
    }
}
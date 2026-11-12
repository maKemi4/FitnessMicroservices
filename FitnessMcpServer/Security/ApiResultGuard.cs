using System.Text;

namespace FitnessMcpServer.Security;

public static class ApiResultGuard
{
    private const int MaxResultBytes = 12000;

    public static string Validate(string json)
    {
        if (Encoding.UTF8.GetByteCount(json) > MaxResultBytes)
        {
            throw new InvalidOperationException(
                "Результат API перевищує дозволені 12000 байтів.");
        }

        PromptInjectionGuard.CheckToolResult(json);

        return json;
    }
}
using System.Text.Json;

namespace FitnessAiClient.Ai;

public sealed class TokenBudget
{
    public const int MaxOutputTokens = 512;

    public long Limit { get; }
    public long Used { get; private set; }

    public TokenBudget()
    {
        var setting = Environment.GetEnvironmentVariable("FITNESS_TOKEN_BUDGET");

        if (string.IsNullOrWhiteSpace(setting))
        {
            Limit = 5000;
        }
        else if (long.TryParse(setting, out var value) && value > 0)
        {
            Limit = value;
        }
        else
        {
            throw new InvalidOperationException(
                "FITNESS_TOKEN_BUDGET має бути додатним цілим числом.");
        }
    }

    public void EnsureCanRequest()
    {
        if (Used >= Limit)
        {
            throw new InvalidOperationException(
                $"Вичерпано бюджет токенів: {Used}/{Limit}. Нові запити заблоковано.");
        }
    }

    public void Record(JsonElement response)
    {
        if (!response.TryGetProperty("usage", out var usage)
            || !usage.TryGetProperty("total_tokens", out var total)
            || !total.TryGetInt64(out var tokens)
            || tokens < 0)
        {
            throw new InvalidOperationException(
                "Google не повернув коректне використання токенів. Виконання зупинено.");
        }

        Used = checked(Used + tokens);

        Console.WriteLine(
            $"[Токени] Запит: {tokens}; усього: {Used}/{Limit}");

        EnsureCanRequest();
    }
}
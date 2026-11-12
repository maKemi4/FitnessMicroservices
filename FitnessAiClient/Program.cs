using System.Text;
using FitnessAiClient.Ai;
using FitnessAiClient.Services;
using FitnessAiClient.Tools;

Console.OutputEncoding = Encoding.UTF8;

var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("Не знайдено GEMINI_API_KEY.");
    return;
}

using var geminiHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
using var apiHttp = new HttpClient
{
    BaseAddress = new Uri("http://localhost:8080"),
    Timeout = TimeSpan.FromSeconds(10)
};

var gemini = new GeminiClient(geminiHttp, apiKey);
var users = new UserProfileApiClient(apiHttp);
var statistics = new StatisticsApiClient(apiHttp);
var tools = new FitnessTools(users, statistics);
var agent = new AgentRunner(gemini, tools);

try
{
    await agent.RunAsync("""
        Знайди користувача з ім'ям Test User.
        Якщо таких користувачів декілька, обери користувача з найменшим ID.
        Потім отримай його статистику тренувань.
        Покажи ім'я, ID, кількість завершених тренувань,
        їхню загальну тривалість і загальну кількість вправ.
        Якщо такого користувача немає, повідом про це.
        """);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Перевищено час очікування відповіді Gemini або API.");
}
catch (Exception exception)
{
    Console.WriteLine($"Помилка: {exception.Message}");
}

# FitnessAiClient — перший інструмент get_users

Консольний клієнт .NET 10, Gemini Interactions API, модель gemini-3.1-flash-lite.
Зовнішні NuGet-пакети не потрібні.

## Встановлення

1. Зробіть резервну копію поточної папки FitnessAiClient за межами рішення.
2. Замініть її папкою FitnessAiClient із цього архіву. Файл рішення й інші сервіси замінювати не потрібно.
3. Залиште контейнери FitnessMicroservices запущеними.
4. У PowerShell з кореневої папки FitnessMicroservices виконайте:

```powershell
dotnet run --project FitnessAiClient
```

Якщо це нове вікно PowerShell, спочатку введіть ключ:

```powershell
$env:GEMINI_API_KEY = [System.Net.NetworkCredential]::new("", (Read-Host "Gemini API key" -AsSecureString)).Password
```

## Файли

| Файл | Відповідальність |
| --- | --- |
| Program.cs | Створення клієнтів, запуск, показ помилок |
| Ai/GeminiClient.cs | HTTP-запити до Gemini |
| Ai/AgentRunner.cs | Цикл виклику інструментів і формування відповіді |
| Services/UserProfileApiClient.cs | GET /api/users через API Gateway |
| Tools/FitnessTools.cs | Опис get_users, перевірка аргументів, маршрутизація виклику |

Очікується: виклик get_users, запит до localhost:8080/api/users та відповідь українською з актуальними користувачами.

Це проміжний крок лабораторної. Є обмеження кількості запитів та викликів інструментів; повний бюджет токенів і захист від prompt injection ще потрібно реалізувати й перевірити.

Перевірка тут: структура проєкту, XML та склад архіву. Збірку й інтеграційний запуск потрібно виконати на комп'ютері з .NET 10 та власним API-ключем.

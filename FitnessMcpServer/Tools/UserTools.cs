using System.ComponentModel;
using FitnessMcpServer.Security;
using FitnessMcpServer.Services;
using ModelContextProtocol.Server;

namespace FitnessMcpServer.Tools;

[McpServerToolType]
public sealed class UserTools(UserProfileApiClient users)
{
    [McpServerTool(Name = "get_users")]
    [Description(
        "Отримує актуальний список користувачів FitnessMicroservices: " +
        "ID, ім'я, зріст і вагу. Використовуй для пошуку користувача за ім'ям.")]
    public async Task<string> GetUsers(
        CancellationToken cancellationToken)
    {
        var json = await users.GetUsersAsync(cancellationToken);

        return ApiResultGuard.Validate(json);
    }

    [McpServerTool(Name = "get_user")]
    [Description("Отримує конкретного користувача за його ID.")]
    public async Task<string> GetUser(
    [Description("Додатний цілий ID користувача.")]
    int userId,
    CancellationToken cancellationToken)
    {
        var json = await users.GetByIdAsync(
            userId,
            cancellationToken);

        return ApiResultGuard.Validate(json);
    }

    [McpServerTool(Name = "create_user")]
    [Description(
        "Створює нового користувача. Змінює дані в базі. " +
        "Використовуй лише за явним запитом користувача на створення.")]
    public async Task<string> CreateUser(
        [Description("Ім'я користувача, від 1 до 100 символів.")]
    string name,
        [Description("Зріст у сантиметрах, додатне число.")]
    double height,
        [Description("Вага в кілограмах, додатне число.")]
    double weight,
        CancellationToken cancellationToken)
    {
        var json = await users.CreateAsync(
            name,
            height,
            weight,
            cancellationToken);

        return ApiResultGuard.Validate(json);
    }

    [McpServerTool(Name = "update_user")]
    [Description(
        "Оновлює ім'я, зріст і вагу наявного користувача. " +
        "Змінює дані в базі. Потрібно передати всі три значення. " +
        "Використовуй лише за явним запитом на зміну.")]
    public async Task<string> UpdateUser(
        [Description("ID користувача, якого потрібно змінити.")]
    int userId,
        [Description("Нове ім'я користувача.")]
    string name,
        [Description("Зріст у сантиметрах.")]
    double height,
        [Description("Вага в кілограмах.")]
    double weight,
        CancellationToken cancellationToken)
    {
        var json = await users.UpdateAsync(
            userId,
            name,
            height,
            weight,
            cancellationToken);

        return ApiResultGuard.Validate(json);
    }

    [McpServerTool(Name = "delete_user")]
    [Description(
        "Видаляє користувача з бази за ID. " +
        "Використовуй лише за явним запитом на видалення конкретного користувача.")]
    public async Task<string> DeleteUser(
        [Description("ID користувача, якого потрібно видалити.")]
    int userId,
        CancellationToken cancellationToken)
    {
        var json = await users.DeleteAsync(
            userId,
            cancellationToken);

        return ApiResultGuard.Validate(json);
    }
}
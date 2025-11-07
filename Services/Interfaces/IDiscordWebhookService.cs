namespace CrudCloud.api.Services;

public interface IDiscordWebhookService
{
    Task SendUserCreatedAsync(string userEmail, string userId, DateTime createdAt);
    Task SendDatabaseCreatedAsync(string databaseName, string databaseType, string userId, string userName);
    Task SendDatabaseDeletedAsync(string databaseName, string databaseType, string userId, string userName);
    Task SendErrorAsync(string errorMessage, string stackTrace, string endpoint, string? userId = null);
    Task SendPlanUpdatedAsync(string userEmail, string userId, string oldPlan, string newPlan);
    Task SendEmailSentAsync(string toEmail, string emailType, bool success, string? error = null);
}
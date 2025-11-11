using CrudCloud.api.Models;
namespace CrudCloud.api.Services;


public interface IEmailService
{
    Task SendEmailVerificationAsync(string toEmail, string userName, string verificationToken);
    Task SendPasswordResetAsync(string toEmail, string userName, string resetToken);
    Task SendPlanUpgradeNotificationAsync(string toEmail, string userName, string oldPlan, string newPlan);
    Task SendWelcomeEmailAsync(string toEmail, string userName);
    Task SendDatabaseCreatedEmailAsync(string toEmail, string userName, DatabaseInstance instance);
    Task SendDatabaseDeletedEmailAsync(string toEmail, string userName, DatabaseInstance instance);
    Task SendPasswordChangedConfirmationAsync(string toEmail, string userName); 
}
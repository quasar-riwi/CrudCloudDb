namespace CrudCloud.api.Services;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string toEmail, string userName, string verificationToken);
    Task SendPasswordResetAsync(string toEmail, string userName, string resetToken);
    Task SendPlanUpgradeNotificationAsync(string toEmail, string userName, string oldPlan, string newPlan);
    Task SendWelcomeEmailAsync(string toEmail, string userName);
}
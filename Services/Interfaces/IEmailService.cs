namespace CrudCloud.api.Services;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string toEmail, string userName, string verificationToken);
    Task SendPasswordResetAsync(string toEmail, string userName, string resetToken);
    Task SendWelcomeEmailAsync(string toEmail, string userName);
    Task SendPlanUpgradeNotificationAsync(string toEmail, string userName, string oldPlan, string newPlan);
    Task SendPaymentConfirmationAsync(string toEmail, string userName, string plan, decimal amount, string paymentId);
}
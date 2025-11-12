using CrudCloud.api.Data.Entities; // ✅ NECESARIO para DatabaseInstance
using System.Threading.Tasks;

namespace CrudCloud.api.Services.Interfaces;

public interface IEmailService
{
    // Autenticación
    Task SendEmailVerificationAsync(string toEmail, string userName, string verificationToken);
    Task SendPasswordResetAsync(string toEmail, string userName, string resetToken);
    Task SendWelcomeEmailAsync(string toEmail, string userName);
    Task SendPasswordResetSuccessAsync(string toEmail, string userName);
    Task SendPasswordChangedConfirmationAsync(string toEmail, string userName);

    // Pagos
    Task SendPaymentConfirmationAsync(string toEmail, string userName, string plan, decimal amount, string paymentId);
    Task SendPlanUpgradeNotificationAsync(string toEmail, string userName, string oldPlan, string newPlan);

    // Base de Datos (Requiere DatabaseInstance)
    Task SendDatabaseCreatedEmailAsync(string toEmail, string userName, DatabaseInstance instance);
    Task SendDatabaseDeletedEmailAsync(string toEmail, string userName, DatabaseInstance instance);
}
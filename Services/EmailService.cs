using CrudCloud.api.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace CrudCloud.api.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;

    public EmailService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        // Construye el mensaje de correo
        var message = new MailMessage
        {
            From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
        };
        message.To.Add(toEmail);

        // Configura el cliente SMTP con los valores de appsettings.json
        using var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
        {
            Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password),
            EnableSsl = _emailSettings.EnableSsl,
        };

        try
        {
            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            // Aquí puedes loguear el error de forma más robusta si lo deseas
            // Pero al no estar en un try-catch en el servicio, la excepción original
            // llegará hasta UserService, donde ya la estás manejando.
            Console.WriteLine($"Fallo crítico al enviar email: {ex.Message}");
            throw; // Re-lanza la excepción para que el servicio que lo llamó se entere.
        }
    }

    public async Task SendEmailVerificationAsync(string toEmail, string userName, string verificationToken)
    {
        
        var verificationLink = $"http://localhost:5173/dashboard/home/verify-email?token={verificationToken}";

        var subject = "¡Confirma tu correo electrónico en CrudCloud!";
        var body = $@"
            <h1>¡Bienvenido a CrudCloud, {userName}!</h1>
            <p>Gracias por registrarte. Por favor, haz clic en el siguiente enlace para verificar tu cuenta:</p>
            <a href='{verificationLink}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Verificar mi cuenta</a>
            <p>Si no te registraste en nuestra plataforma, por favor ignora este correo.</p>
            <p>El equipo de CrudCloud</p>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendPasswordResetAsync(string toEmail, string userName, string resetToken)
    {
        // TODO: Reemplaza con la URL de tu frontend para resetear contraseña
        var resetLink = $"https://tu-frontend.com/reset-password?token={resetToken}";
        
        var subject = "Solicitud de recuperación de contraseña";
        var body = $@"
            <h1>Hola, {userName}</h1>
            <p>Hemos recibido una solicitud para restablecer tu contraseña. Haz clic en el siguiente enlace:</p>
            <a href='{resetLink}'>Restablecer contraseña</a>
            <p>Si no solicitaste esto, ignora este correo.</p>";

        await SendEmailAsync(toEmail, subject, body);
    }
    
    public async Task SendWelcomeEmailAsync(string toEmail, string userName)
    {
        var subject = $"¡Bienvenido oficialmente a CrudCloud, {userName}!";
        var body = $@"
            <h1>¡Tu cuenta ha sido verificada!</h1>
            <p>Hola {userName},</p>
            <p>Te damos la bienvenida a CrudCloud. Tu correo ha sido verificado y tu cuenta está activa. ¡Ya puedes iniciar sesión y empezar a usar nuestra plataforma!</p>
            <p>Saludos,<br>El equipo de CrudCloud</p>";

        await SendEmailAsync(toEmail, subject, body);
    }


    public async Task SendPlanUpgradeNotificationAsync(string toEmail, string userName, string oldPlan, string newPlan)
    {
        var subject = "¡Tu plan en CrudCloud ha cambiado!";
        var body = $@"
            <h1>Cambio de plan</h1>
            <p>Hola {userName},</p>
            <p>Te informamos que tu plan ha sido actualizado de <strong>{oldPlan}</strong> a <strong>{newPlan}</strong>.</p>
            <p>Gracias por confiar en nosotros.</p>";

        await SendEmailAsync(toEmail, subject, body);
    }
}
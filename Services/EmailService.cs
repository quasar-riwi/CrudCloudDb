using CrudCloud.api.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace CrudCloud.api.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly IDiscordWebhookService _discordWebhookService;

    public EmailService(IOptions<EmailSettings> emailSettings, IDiscordWebhookService discordWebhookService)
    {
        _emailSettings = emailSettings.Value;
        _discordWebhookService = discordWebhookService;
    }

    // --- ✅ CORREGIDO: Se añaden '?' para resolver advertencias de nulabilidad ---
    private string GetEmailTemplate(string title, string userName, string content, string? buttonText = null, string? buttonUrl = null)
    {
        var buttonHtml = buttonText != null && buttonUrl != null ? 
            $@"<div class='button-container'>
                <a href='{buttonUrl}' class='button'>{buttonText}</a>
            </div>" : "";

        // --- TU PLANTILLA HTML COMPLETA Y CORRECTA ---
        return $@"<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{title}</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: linear-gradient(135deg, #0a0a1a 0%, #1a1a2e 50%, #16213e 100%); color: #e0e0e0; line-height: 1.6; min-height: 100vh; position: relative; overflow-x: hidden; }}
        body::before {{ content: ''; position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: radial-gradient(circle at 20% 30%, rgba(0, 123, 255, 0.1) 0%, transparent 50%), radial-gradient(circle at 80% 70%, rgba(100, 65, 165, 0.1) 0%, transparent 50%), radial-gradient(circle at 40% 80%, rgba(41, 217, 194, 0.05) 0%, transparent 50%); pointer-events: none; z-index: -1; }}
        .stars {{ position: fixed; top: 0; left: 0; width: 100%; height: 100%; pointer-events: none; z-index: -1; }}
        .star {{ position: absolute; background: white; border-radius: 50%; animation: twinkle 4s infinite; }}
        @keyframes twinkle {{ 0%, 100% {{ opacity: 0.3; }} 50% {{ opacity: 1; }} }}
        .container {{ max-width: 600px; margin: 40px auto; background: rgba(15, 15, 35, 0.95); border-radius: 20px; overflow: hidden; box-shadow: 0 20px 40px rgba(0, 0, 0, 0.6), 0 0 80px rgba(0, 123, 255, 0.1), inset 0 0 60px rgba(41, 217, 194, 0.05); border: 1px solid rgba(64, 224, 208, 0.2); backdrop-filter: blur(10px); position: relative; }}
        .container::before {{ content: ''; position: absolute; top: 0; left: 0; right: 0; height: 1px; background: linear-gradient(90deg, transparent, rgba(64, 224, 208, 0.4), rgba(0, 123, 255, 0.4), rgba(100, 65, 165, 0.4), transparent); }}
        .header {{ background: linear-gradient(135deg, rgba(0, 20, 40, 0.9) 0%, rgba(0, 40, 80, 0.9) 50%, rgba(25, 25, 112, 0.9) 100%); padding: 40px 30px; text-align: center; border-bottom: 1px solid rgba(64, 224, 208, 0.3); position: relative; overflow: hidden; }}
        .header::before {{ content: ''; position: absolute; top: -50%; left: -50%; width: 200%; height: 200%; background: radial-gradient(circle, rgba(41, 217, 194, 0.1) 0%, transparent 70%); animation: rotate 20s linear infinite; }}
        @keyframes rotate {{ from {{ transform: rotate(0deg); }} to {{ transform: rotate(360deg); }} }}
        .logo {{ font-size: 32px; font-weight: bold; color: #fff; margin-bottom: 10px; text-shadow: 0 0 20px rgba(64, 224, 208, 0.7), 0 0 40px rgba(0, 123, 255, 0.5); position: relative; letter-spacing: 1px; }}
        .tagline {{ font-size: 14px; color: rgba(255, 255, 255, 0.8); letter-spacing: 0.5px; position: relative; }}
        .content {{ padding: 50px 40px; position: relative; }}
        .greeting {{ font-size: 28px; color: #ffffff; margin-bottom: 25px; font-weight: 700; text-shadow: 0 0 20px rgba(64, 224, 208, 0.5), 0 0 40px rgba(0, 123, 255, 0.3); }}
        .message {{ font-size: 16px; color: #c0c0c0; margin-bottom: 30px; line-height: 1.7; }}
        .message p {{ margin-bottom: 20px; }}
        .button-container {{ text-align: center; margin: 40px 0 30px; }}
        .button {{ display: inline-block; background: linear-gradient(135deg, #0d1a2b 0%, #1a2d4a 100%); color: #ffffff; padding: 16px 40px; text-decoration: none; border-radius: 12px; font-weight: 600; font-size: 16px; transition: all 0.4s ease; box-shadow: 0 8px 25px rgba(41, 217, 194, 0.3), 0 0 20px rgba(64, 224, 208, 0.2); border: none; position: relative; overflow: hidden; letter-spacing: 0.5px; text-shadow: 0 1px 2px rgba(0, 0, 0, 0.5); }}
        .button::before {{ content: ''; position: absolute; top: 0; left: -100%; width: 100%; height: 100%; background: linear-gradient(90deg, transparent, rgba(64, 224, 208, 0.2), transparent); transition: left 0.6s; }}
        .button:hover {{ background: linear-gradient(135deg, #1a2d4a 0%, #243b5c 100%); transform: translateY(-2px); box-shadow: 0 12px 35px rgba(41, 217, 194, 0.4), 0 0 30px rgba(64, 224, 208, 0.3); color: #f0f8ff; }}
        .button:hover::before {{ left: 100%; }}
        .footer {{ background: rgba(10, 10, 25, 0.95); padding: 35px 25px; text-align: center; border-top: 1px solid rgba(64, 224, 208, 0.2); position: relative; }}
        .footer::before {{ content: ''; position: absolute; top: 0; left: 0; right: 0; height: 1px; background: linear-gradient(90deg, transparent, rgba(64, 224, 208, 0.3), transparent); }}
        .footer-text {{ font-size: 12px; color: #888; margin-bottom: 15px; line-height: 1.5; }}
        .social-links {{ margin-top: 20px; }}
        .social-link {{ color: #40e0d0; text-decoration: none; margin: 0 15px; font-size: 12px; transition: all 0.3s ease; position: relative; }}
        .social-link::after {{ content: ''; position: absolute; bottom: -2px; left: 0; width: 0; height: 1px; background: #40e0d0; transition: width 0.3s ease; }}
        .social-link:hover {{ color: #007bff; text-shadow: 0 0 10px rgba(64, 224, 208, 0.5); }}
        .social-link:hover::after {{ width: 100%; }}
        .highlight {{ background: rgba(30, 30, 60, 0.7); padding: 20px; border-radius: 12px; border-left: 4px solid #40e0d0; margin: 25px 0; box-shadow: inset 0 0 20px rgba(0, 0, 0, 0.3), 0 5px 15px rgba(0, 0, 0, 0.2); border: 1px solid rgba(64, 224, 208, 0.2); }}
        .plan-change {{ background: linear-gradient(135deg, rgba(25, 135, 84, 0.2) 0%, rgba(41, 217, 194, 0.2) 100%); color: #40e0d0; padding: 25px; border-radius: 15px; text-align: center; margin: 25px 0; border: 1px solid rgba(41, 217, 194, 0.3); box-shadow: 0 0 30px rgba(41, 217, 194, 0.2), inset 0 0 30px rgba(41, 217, 194, 0.1); }}
        .plan-change strong {{ color: #fff; font-size: 18px; text-shadow: 0 0 10px rgba(41, 217, 194, 0.5); }}
        @media (max-width: 600px) {{ .container {{ margin: 15px; border-radius: 16px; }} .content {{ padding: 35px 25px; }} .header {{ padding: 30px 20px; }} .greeting {{ font-size: 24px; }} }}
    </style>
</head>
<body>
    <div class='stars' id='stars'></div>
    <div class='container'>
        <div class='header'>
            <div class='logo'>CrudCloud</div>
            <div class='tagline'>Explora el universo de tus bases de datos</div>
        </div>
        <div class='content'>
            <div class='greeting'>¡Hola, {userName}!</div>
            <div class='message'>
                {content}
            </div>
            {buttonHtml}
        </div>
        <div class='footer'>
            <div class='footer-text'>
                © 2025 CrudCloud. Todos los derechos reservados.<br>
                Este es un email automático, por favor no respondas a este mensaje.
            </div>
            <div class='social-links'>
                <a href='https://quasar.andrescortes.dev' class='social-link'>Sitio Web</a>
                <a href='#' class='social-link'>Soporte</a>
                <a href='#' class='social-link'>Contacto</a>
            </div>
        </div>
    </div>
    <script>
        document.addEventListener('DOMContentLoaded', function() {{ const starsContainer = document.getElementById('stars'); const starCount = 100; for (let i = 0; i < starCount; i++) {{ const star = document.createElement('div'); star.className = 'star'; const left = Math.random() * 100; const top = Math.random() * 100; const size = Math.random() * 2 + 1; const opacity = Math.random() * 0.7 + 0.3; const delay = Math.random() * 4; star.style.cssText = `left: ${{left}}%; top: ${{top}}%; width: ${{size}}px; height: ${{size}}px; opacity: ${{opacity}}; animation-delay: ${{delay}}s;`; starsContainer.appendChild(star); }} }});
    </script>
</body>
</html>";
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body, string emailType)
    {
        var message = new MailMessage
        {
            From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
        {
            Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password),
            EnableSsl = _emailSettings.EnableSsl,
        };

        try
        {
            await client.SendMailAsync(message);
            await _discordWebhookService.SendEmailSentAsync(toEmail, emailType, true);
        }
        catch (Exception ex)
        {
            await _discordWebhookService.SendEmailSentAsync(toEmail, emailType, false, ex.Message);
            Console.WriteLine($"Fallo crítico al enviar email: {ex.Message}");
            throw;
        }
    }

    public async Task SendEmailVerificationAsync(string toEmail, string userName, string verificationToken)
    {
        var verificationLink = $"https://quasar.andrescortes.dev/verify-email?token={verificationToken}";
        var subject = "Verifica tu cuenta - CrudCloud";
        var content = @"<p>¡Estamos emocionados de tenerte en CrudCloud! Para comenzar a crear tus bases de datos en la nube, necesitamos que verifiques tu dirección de correo electrónico.</p><div class='highlight'><strong>Importante:</strong> Tu cuenta no estará completamente activa hasta que verifiques tu email.</div><p>Haz clic en el botón de abajo para completar la verificación:</p>";
        var body = GetEmailTemplate("Verificación de Cuenta", userName, content, "Verificar Mi Cuenta", verificationLink);
        await SendEmailAsync(toEmail, subject, body, "Email Verification");
    }

    public async Task SendPasswordResetAsync(string toEmail, string userName, string resetToken)
    {
        var resetLink = $"https://quasar.andrescortes.dev/reset-password?token={resetToken}";
		var subject = "Restablecer Contraseña - CrudCloud";
        var content = @"<p>Hemos recibido una solicitud para restablecer la contraseña de tu cuenta.</p><div class='highlight'><strong>Seguridad:</strong> Este enlace expirará en 1 hora por tu seguridad.</div><p>Si no solicitaste este cambio, puedes ignorar este mensaje de manera segura.</p><p>Para crear una nueva contraseña, haz clic en el botón:</p>";
        var body = GetEmailTemplate("Restablecer Contraseña", userName, content, "Crear Nueva Contraseña", resetLink);
        await SendEmailAsync(toEmail, subject, body, "Password Reset");
    }
    
    public async Task SendWelcomeEmailAsync(string toEmail, string userName)
    {
        var subject = "¡Bienvenido a CrudCloud!";
        var content = @"<p>¡Felicidades! Tu cuenta ha sido verificada exitosamente y ahora estás listo para comenzar.</p><div class='highlight'><strong>¿Qué puedes hacer ahora?</strong><br>• Crear bases de datos MySQL, PostgreSQL, MongoDB<br>• Gestionar múltiples instancias<br>• Escalar según tus necesidades</div><p>Inicia sesión en tu cuenta y descubre todas las funcionalidades que tenemos para ti.</p>";
        var body = GetEmailTemplate("Cuenta Activada", userName, content);
        await SendEmailAsync(toEmail, subject, body, "Welcome Email");
    }

    public async Task SendPlanUpgradeNotificationAsync(string toEmail, string userName, string oldPlan, string newPlan)
    {
        var subject = "¡Tu plan ha sido actualizado! - CrudCloud";
        var content = $@"<p>Te informamos que tu suscripción ha sido actualizada exitosamente.</p><div class='plan-change'><strong>Cambio de Plan</strong><br><span style='font-size: 18px;'>De <strong>{oldPlan}</strong> a <strong>{newPlan}</strong></span></div><p>Los nuevos límites y características de tu plan están ahora activos. ¡Disfruta de las mejoras!</p>";
        var body = GetEmailTemplate("Plan Actualizado", userName, content);
        await SendEmailAsync(toEmail, subject, body, "Plan Upgrade");
    }
    
    public async Task SendDatabaseCreatedEmailAsync(string toEmail, string userName, DatabaseInstance instance)
    {
        var subject = $"¡Tu base de datos {instance.Motor} está lista!";
        var content = $@"<p>Hemos creado con éxito tu nueva base de datos. Aquí están los detalles para que puedas conectarte y empezar a construir:</p><div class='highlight'><p><strong>Motor:</strong> {instance.Motor}</p><p><strong>Host:</strong> {instance.Host}</p><p><strong>Puerto:</strong> {instance.Puerto}</p><p><strong>Nombre de la Base de Datos:</strong> {instance.Nombre}</p><p><strong>Usuario:</strong> {instance.UsuarioDb}</p><p><strong>Contraseña:</strong> <code>{instance.Contraseña}</code></p></div><p>Guarda estas credenciales en un lugar seguro. ¡Estamos emocionados por ver lo que crearás!</p>";
        var body = GetEmailTemplate("Base de Datos Creada", userName, content, "Ir a mi Panel", "https://quasar.andrescortes.dev/dashboard");
        await SendEmailAsync(toEmail, subject, body, "Database Created");
    }

    public async Task SendDatabaseDeletedEmailAsync(string toEmail, string userName, DatabaseInstance instance)
    {
        var subject = $"Confirmación de eliminación de la base de datos";
        var content = $@"<p>Te confirmamos que la siguiente base de datos ha sido eliminada permanentemente de tu cuenta:</p><div class='highlight'><p><strong>Nombre de la Base de Datos:</strong> {instance.Nombre}</p><p><strong>Motor:</strong> {instance.Motor}</p></div><p>Esta acción no se puede deshacer. Si esto fue un error o necesitas ayuda, por favor contacta a nuestro equipo de soporte.</p>";
        var body = GetEmailTemplate("Base de Datos Eliminada", userName, content);
        await SendEmailAsync(toEmail, subject, body, "Database Deleted");
    }
    
    // --- ✅ MÉTODO AÑADIDO PARA CUMPLIR CON LA INTERFAZ ---
    public async Task SendPasswordChangedConfirmationAsync(string toEmail, string userName)
    {
        var subject = "Confirmación de cambio de contraseña - CrudCloud";
        
        var content = $@"<p>Te confirmamos que la contraseña de tu cuenta ha sido cambiada exitosamente.</p>
            <div class='highlight'>
                <strong>Nota de seguridad:</strong> Si no realizaste este cambio, por favor, contacta a nuestro equipo de soporte inmediatamente para asegurar tu cuenta.
            </div>
            <p>Puedes iniciar sesión con tu nueva contraseña ahora.</p>";

        var body = GetEmailTemplate("Contraseña Actualizada", userName, content, "Iniciar Sesión", "https://quasar.andrescortes.dev/login");
        await SendEmailAsync(toEmail, subject, body, "Password Changed Confirmation");
    }
}
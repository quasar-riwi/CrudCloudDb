using CrudCloud.api.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CrudCloud.api.Services; 

public class EmailService : IEmailService 
{
    private readonly EmailSettings _emailSettings;
    private readonly IConfiguration _configuration;

    public EmailService(IOptions<EmailSettings> emailSettings, IConfiguration configuration)
    {
        _emailSettings = emailSettings.Value;
        _configuration = configuration;
    }
    public async Task SendEmailVerificationAsync(string toEmail, string userName, string verificationToken)
    {
        var frontendUrl = _configuration["AppSettings:FrontendUrl"];
        var verificationLink = $"{frontendUrl}/verify-email?token={verificationToken}";

        var subject = "Verifica tu cuenta - CrudCloud Platform";
        var body = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                    .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                    .button {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                    .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>🚀 Bienvenido a CrudCloud Platform</h1>
                    </div>
                    <div class='content'>
                        <h2>¡Hola {userName}!</h2>
                        <p>Gracias por registrarte en CrudCloud Platform. Para activar tu cuenta y comenzar a crear instancias de bases de datos, por favor verifica tu correo electrónico.</p>
                        
                        <p>Haz clic en el siguiente botón para verificar tu cuenta:</p>
                        
                        <div style='text-align: center;'>
                            <a href='{verificationLink}' class='button'>Verificar mi cuenta</a>
                        </div>
                        
                        <p>O copia y pega este enlace en tu navegador:</p>
                        <p style='background: #fff; padding: 10px; border-radius: 5px; word-break: break-all;'>{verificationLink}</p>
                        
                        <p><strong>Este enlace expirará en 24 horas.</strong></p>
                        
                        <hr style='margin: 20px 0; border: none; border-top: 1px solid #ddd;'>
                        
                        <h3>📦 Tu Plan Actual: Gratis</h3>
                        <p>Con tu plan gratuito puedes crear hasta <strong>2 bases de datos por motor</strong>:</p>
                        <ul>
                            <li>MySQL: 2 instancias</li>
                            <li>PostgreSQL: 2 instancias</li>
                            <li>MongoDB: 2 instancias</li>
                            <li>SQL Server: 2 instancias</li>
                            <li>Redis: 2 instancias</li>
                            <li>Cassandra: 2 instancias</li>
                        </ul>
                        
                        <p>Si no solicitaste esta cuenta, puedes ignorar este correo.</p>
                    </div>
                    <div class='footer'>
                        <p>© 2025 CrudCloud Platform. Todos los derechos reservados.</p>
                        <p>voyager.andrescortes.dev</p>
                    </div>
                </div>
            </body>
            </html>
        ";

        await SendEmailAsync(toEmail, subject, body);
    }

    /// <summary>
    /// Envía email de recuperación de contraseña
    /// </summary>
    public async Task SendPasswordResetAsync(string toEmail, string userName, string resetToken)
    {
        var frontendUrl = _configuration["AppSettings:FrontendUrl"];
        var resetLink = $"{frontendUrl}/reset-password?token={resetToken}";

        var subject = "Recuperación de contraseña - CrudCloud Platform";
        var body = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                    .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                    .button {{ display: inline-block; padding: 12px 30px; background: #f5576c; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                    .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
                    .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>🔑 Recuperación de Contraseña</h1>
                    </div>
                    <div class='content'>
                        <h2>Hola {userName},</h2>
                        <p>Recibimos una solicitud para restablecer la contraseña de tu cuenta en CrudCloud Platform.</p>
                        
                        <p>Haz clic en el siguiente botón para crear una nueva contraseña:</p>
                        
                        <div style='text-align: center;'>
                            <a href='{resetLink}' class='button'>Restablecer mi contraseña</a>
                        </div>
                        
                        <p>O copia y pega este enlace en tu navegador:</p>
                        <p style='background: #fff; padding: 10px; border-radius: 5px; word-break: break-all;'>{resetLink}</p>
                        
                        <div class='warning'>
                            <strong>⚠️ Importante:</strong>
                            <ul>
                                <li>Este enlace expirará en <strong>24 horas</strong></li>
                                <li>Solo puede usarse una vez</li>
                                <li>Si no solicitaste este cambio, ignora este correo</li>
                            </ul>
                        </div>
                        
                        <p>Por tu seguridad, nunca compartas este enlace con nadie.</p>
                    </div>
                    <div class='footer'>
                        <p>© 2025 CrudCloud Platform. Todos los derechos reservados.</p>
                        <p>Si no solicitaste este cambio, tu cuenta está segura. Puedes ignorar este mensaje.</p>
                    </div>
                </div>
            </body>
            </html>
        ";

        await SendEmailAsync(toEmail, subject, body);
    }

    /// <summary>
    /// Envía notificación de cambio de plan
    /// </summary>
    public async Task SendPlanUpgradeNotificationAsync(string toEmail, string userName, string oldPlan, string newPlan)
    {
        var limits = new Dictionary<string, int>
        {
            { "Gratis", 2 },
            { "Intermedio", 5 },
            { "Avanzado", 10 }
        };

        var newLimit = limits[newPlan];

        var subject = $"¡Plan actualizado a {newPlan}! - CrudCloud Platform";
        var body = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                    .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                    .plan-card {{ background: white; padding: 20px; border-radius: 10px; margin: 20px 0; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
                    .success {{ background: #d4edda; border-left: 4px solid #28a745; padding: 15px; margin: 20px 0; }}
                    .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>🎉 ¡Felicitaciones {userName}!</h1>
                    </div>
                    <div class='content'>
                        <div class='success'>
                            <strong>✅ Tu plan ha sido actualizado exitosamente</strong>
                        </div>
                        
                        <div class='plan-card'>
                            <h3 style='color: #11998e;'>📊 Cambio de Plan</h3>
                            <p><strong>Plan anterior:</strong> {oldPlan}</p>
                            <p><strong>Plan nuevo:</strong> {newPlan}</p>
                        </div>
                        
                        <h3>🚀 Nuevos Límites de tu Plan {newPlan}:</h3>
                        <p>Ahora puedes crear hasta <strong>{newLimit} bases de datos por motor</strong>:</p>
                        <ul>
                            <li>MySQL: {newLimit} instancias</li>
                            <li>PostgreSQL: {newLimit} instancias</li>
                            <li>MongoDB: {newLimit} instancias</li>
                            <li>SQL Server: {newLimit} instancias</li>
                            <li>Redis: {newLimit} instancias</li>
                            <li>Cassandra: {newLimit} instancias</li>
                        </ul>
                        
                        <p style='margin-top: 30px;'>¡Comienza a crear más instancias de bases de datos y escala tus proyectos!</p>
                        
                        <div style='text-align: center; margin-top: 30px;'>
                            <a href='https://voyager.andrescortes.dev/dashboard' style='display: inline-block; padding: 12px 30px; background: #11998e; color: white; text-decoration: none; border-radius: 5px;'>Ir al Dashboard</a>
                        </div>
                    </div>
                    <div class='footer'>
                        <p>© 2025 CrudCloud Platform. Todos los derechos reservados.</p>
                        <p>Gracias por confiar en nosotros 💚</p>
                    </div>
                </div>
            </body>
            </html>
        ";

        await SendEmailAsync(toEmail, subject, body);
    }

    /// <summary>
    /// Envía email de bienvenida (opcional, después de verificar)
    /// </summary>
    public async Task SendWelcomeEmailAsync(string toEmail, string userName)
    {
        var subject = "¡Cuenta verificada! - CrudCloud Platform";
        var body = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                    .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                    .button {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                    .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>✅ ¡Tu cuenta está activa!</h1>
                    </div>
                    <div class='content'>
                        <h2>¡Hola {userName}!</h2>
                        <p>Tu cuenta ha sido verificada exitosamente. Ya puedes comenzar a usar CrudCloud Platform.</p>
                        
                        <h3>🎯 Próximos pasos:</h3>
                        <ol>
                            <li>Inicia sesión en tu cuenta</li>
                            <li>Explora el dashboard</li>
                            <li>Crea tu primera instancia de base de datos</li>
                            <li>Gestiona tus proyectos desde un solo lugar</li>
                        </ol>
                        
                        <div style='text-align: center;'>
                            <a href='https://voyager.andrescortes.dev/login' class='button'>Iniciar Sesión</a>
                        </div>
                        
                        <p style='margin-top: 30px;'>Si tienes alguna pregunta, no dudes en contactarnos.</p>
                    </div>
                    <div class='footer'>
                        <p>© 2025 CrudCloud Platform. Todos los derechos reservados.</p>
                    </div>
                </div>
            </body>
            </html>
        ";

        await SendEmailAsync(toEmail, subject, body);
    }

    /// <summary>
    /// Método privado para enviar emails con MailKit
    /// </summary>
    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            // Conectar al servidor SMTP
            await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
            
            // Autenticar
            await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
            
            // Enviar email
            await client.SendAsync(message);
            
            // Desconectar
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            // Log del error (puedes usar ILogger aquí)
            throw new InvalidOperationException($"Error al enviar email: {ex.Message}", ex);
        }
    }
}
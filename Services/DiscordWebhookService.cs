using CrudCloud.api.Models;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace CrudCloud.api.Services;

public class DiscordWebhookService : IDiscordWebhookService
{
    private readonly HttpClient _httpClient;
    private readonly DiscordWebhookSettings _discordSettings;
    private readonly ILogger<DiscordWebhookService> _logger;

    public DiscordWebhookService(
        HttpClient httpClient, 
        IOptions<DiscordWebhookSettings> discordSettings,
        ILogger<DiscordWebhookService> logger)
    {
        _httpClient = httpClient;
        _discordSettings = discordSettings.Value;
        _logger = logger;
    }

    public async Task SendUserCreatedAsync(string userEmail, string userId, DateTime createdAt)
    {
        var embed = new
        {
            title = "🟢 USER CREATED",
            description = "New user registered in the platform",
            color = 3066993, // Green
            fields = new[]
            {
                new { name = "👤 User", value = userEmail, inline = true },
                new { name = "🆔 ID", value = userId, inline = true },
                new { name = "📅 Date", value = createdAt.ToString("dd/MM/yyyy HH:mm"), inline = true }
            },
            timestamp = DateTime.UtcNow
        };

        await SendToDiscord(embed, _discordSettings.AuthEventsWebhookUrl);
    }

    public async Task SendDatabaseCreatedAsync(string databaseName, string databaseType, string userId, string userName)
    {
        var embed = new
        {
            title = "⚙️ DATABASE CREATED",
            description = "New database instance created",
            color = 3447003, // Blue
            fields = new[]
            {
                new { name = "🧱 Name", value = databaseName, inline = true },
                new { name = "🔧 Type", value = databaseType, inline = true },
                new { name = "👤 User", value = $"{userName} ({userId})", inline = true }
            },
            timestamp = DateTime.UtcNow
        };

        await SendToDiscord(embed, _discordSettings.DbInstancesEventsWebhookUrl);
    }

    public async Task SendDatabaseDeletedAsync(string databaseName, string databaseType, string userId, string userName)
    {
        var embed = new
        {
            title = "🗑️ DATABASE DELETED",
            description = "Database instance deleted",
            color = 15105570, // Orange
            fields = new[]
            {
                new { name = "🧱 Name", value = databaseName, inline = true },
                new { name = "🔧 Type", value = databaseType, inline = true },
                new { name = "👤 User", value = $"{userName} ({userId})", inline = true }
            },
            timestamp = DateTime.UtcNow
        };

        await SendToDiscord(embed, _discordSettings.DbInstancesEventsWebhookUrl);
    }

    public async Task SendErrorAsync(string errorMessage, string stackTrace, string endpoint, string? userId = null)
    {
        var embed = new
        {
            title = "💥 PRODUCTION ERROR",
            description = "Critical exception captured",
            color = 15158332, // Red
            fields = new[]
            {
                new { name = "❌ Error", value = errorMessage.Length > 500 ? errorMessage.Substring(0, 500) + "..." : errorMessage, inline = false },
                new { name = "🌐 Endpoint", value = endpoint, inline = true },
                new { name = "👤 User", value = userId ?? "Not authenticated", inline = true }
            },
            timestamp = DateTime.UtcNow
        };

        await SendToDiscord(embed, _discordSettings.SystemErrorsWebhookUrl);
    }

    // ✅ MÉTODO FALTANTE - AGREGAR ESTE
    public async Task SendPlanUpdatedAsync(string userEmail, string userId, string oldPlan, string newPlan, decimal? amount = null)
    {
        var amountText = amount.HasValue ? $"${amount.Value:N2} COP" : "N/A";
        
        var embed = new
        {
            title = "💰 PLAN UPDATED",
            description = "User changed subscription plan",
            color = 10181046, // Purple
            fields = new[]
            {
                new { name = "👤 User", value = userEmail, inline = true },
                new { name = "🆔 ID", value = userId, inline = true },
                new { name = "📊 Previous Plan", value = oldPlan, inline = true },
                new { name = "📈 New Plan", value = newPlan, inline = true },
                new { name = "💰 Amount", value = amountText, inline = true }
            },
            timestamp = DateTime.UtcNow
        };

        await SendToDiscord(embed, _discordSettings.PaymentEventsWebhookUrl);
    }

    public async Task SendEmailSentAsync(string toEmail, string emailType, bool success, string? error = null)
    {
        var status = success ? "✅ SENT" : "❌ FAILED";
        var color = success ? 3066993 : 15158332; // Green or Red

        var embed = new
        {
            title = $"✉️ EMAIL {status}",
            description = success ? "Email sent successfully" : "Email sending failed",
            color = color,
            fields = new[]
            {
                new { name = "📧 To", value = toEmail, inline = true },
                new { name = "📋 Type", value = emailType, inline = true },
                new { name = "🕒 Date", value = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm"), inline = true }
            },
            timestamp = DateTime.UtcNow
        };

        // If failed, add error information
        if (!success && !string.IsNullOrEmpty(error))
        {
            var fieldsList = embed.fields.ToList();
            fieldsList.Add(new { name = "🚨 Error", value = error.Length > 500 ? error.Substring(0, 500) + "..." : error, inline = false });
            embed = new { embed.title, embed.description, embed.color, fields = fieldsList.ToArray(), embed.timestamp };
        }

        await SendToDiscord(embed, _discordSettings.EmailValidationWebhookUrl);
    }

    // ✅ MÉTODOS DE PAGOS - AGREGAR ESTOS
    public async Task SendPaymentCreatedAsync(string userEmail, string userId, string plan, decimal amount)
    {
        var embed = new
        {
            title = "💳 PAYMENT CREATED",
            description = "New payment initiated by user",
            color = 3447003, // Blue
            fields = new[]
            {
                new { name = "👤 User", value = userEmail, inline = true },
                new { name = "🆔 ID", value = userId, inline = true },
                new { name = "📦 Plan", value = plan, inline = true },
                new { name = "💰 Amount", value = $"${amount:N2} COP", inline = true }
            },
            timestamp = DateTime.UtcNow
        };

        await SendToDiscord(embed, _discordSettings.PaymentEventsWebhookUrl);
    }

    public async Task SendPaymentRejectedAsync(string userEmail, string userId, string plan, decimal amount)
    {
        var embed = new
        {
            title = "❌ PAYMENT REJECTED",
            description = "User payment was rejected",
            color = 15158332, // Red
            fields = new[]
            {
                new { name = "👤 User", value = userEmail, inline = true },
                new { name = "🆔 ID", value = userId, inline = true },
                new { name = "📦 Plan", value = plan, inline = true },
                new { name = "💰 Amount", value = $"${amount:N2} COP", inline = true }
            },
            timestamp = DateTime.UtcNow
        };

        await SendToDiscord(embed, _discordSettings.PaymentEventsWebhookUrl);
    }

    public async Task SendSubscriptionCancelledAsync(string userEmail, string userId, string plan)
    {
        var embed = new
        {
            title = "🚫 SUBSCRIPTION CANCELLED",
            description = "User cancelled their subscription",
            color = 15105570, // Orange
            fields = new[]
            {
                new { name = "👤 User", value = userEmail, inline = true },
                new { name = "🆔 ID", value = userId, inline = true },
                new { name = "📦 Plan", value = plan, inline = true }
            },
            timestamp = DateTime.UtcNow
        };

        await SendToDiscord(embed, _discordSettings.PaymentEventsWebhookUrl);
    }

    private async Task SendToDiscord(object embed, string webhookUrl)
    {
        if (string.IsNullOrEmpty(webhookUrl))
        {
            _logger.LogWarning("Discord webhook URL not configured");
            return;
        }

        try
        {
            var payload = new
            {
                embeds = new[] { embed }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(webhookUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to send webhook to Discord: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending webhook to Discord");
            // Don't throw exception to avoid breaking main flow
        }
    }
}
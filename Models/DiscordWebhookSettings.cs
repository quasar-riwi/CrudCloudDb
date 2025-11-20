namespace CrudCloud.api.Models;

public class DiscordWebhookSettings
{
    public string AuthEventsWebhookUrl { get; set; } = string.Empty;
    public string DbInstancesEventsWebhookUrl { get; set; } = string.Empty;
    public string PaymentEventsWebhookUrl { get; set; } = string.Empty;
    public string SystemErrorsWebhookUrl { get; set; } = string.Empty;
    public string EmailValidationWebhookUrl { get; set; } = string.Empty;
}
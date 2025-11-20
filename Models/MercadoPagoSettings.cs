namespace CrudCloud.api.Models;

public class MercadoPagoSettings
{
    public string AccessToken { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public bool UseSandbox { get; set; }
    public string WebhookSecret { get; set; } = string.Empty;
    public string WebhookBaseUrl { get; set; } = string.Empty;
    public string FrontendBaseUrl { get; set; } = string.Empty;
}
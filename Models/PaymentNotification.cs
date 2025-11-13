using System.Text.Json;
using System.Text.Json.Serialization;

namespace CrudCloud.api.Models;

public class PaymentNotification
{
    // ✅ CORRECCIÓN FINAL: Usamos JsonElement para propiedades ambiguas (id, user_id)
    [JsonPropertyName("id")]
    public JsonElement NotificationId { get; set; } // Lo leeremos como string/long
    
    [JsonPropertyName("date_created")]
    public DateTime DateCreated { get; set; }

    [JsonPropertyName("user_id")]
    public JsonElement UserIdElement { get; set; } // Lo leeremos como string/long
    
    [JsonPropertyName("live_mode")]
    public bool LiveMode { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("data")] 
    public JsonElement Data { get; set; } 
    
    // Propiedades calculadas para acceso simple
    [JsonIgnore]
    public string ResourceId 
    {
        get
        {
            if (Data.ValueKind == JsonValueKind.Object && Data.TryGetProperty("id", out var idElement))
            {
                return idElement.ValueKind == JsonValueKind.Number ? idElement.GetInt64().ToString() : idElement.GetString() ?? string.Empty;
            }
            return string.Empty;
        }
    }
}
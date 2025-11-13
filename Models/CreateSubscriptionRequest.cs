using System.ComponentModel.DataAnnotations;

namespace CrudCloud.api.Models;

public class CreateSubscriptionRequest
{
    [Required]
    public string Plan { get; set; } = string.Empty;
    
    public string? PayerEmail { get; set; }
}
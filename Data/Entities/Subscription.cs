using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CrudCloud.api.Models;

namespace CrudCloud.api.Data.Entities;

[Table("Subscriptions")]
public class Subscription
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [ForeignKey("UserId")]
    public virtual User User { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string MercadoPagoSubscriptionId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Plan { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal MonthlyPrice { get; set; }
    
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime NextBillingDate { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [NotMapped]
    public bool IsActive => Status == "authorized" && (EndDate == null || EndDate > DateTime.UtcNow);
}
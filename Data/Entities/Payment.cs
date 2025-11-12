using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CrudCloud.api.Models; 

namespace CrudCloud.api.Data.Entities;

[Table("Payments")]
public class Payment
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [ForeignKey("UserId")]
    public virtual User User { get; set; } 
    
    [Required]
    [MaxLength(50)]
    public string MercadoPagoPaymentId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Plan { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }
    
    [MaxLength(10)]
    public string Currency { get; set; } = "COP";
    
    [MaxLength(255)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    [MaxLength(50)]
    public string PaymentType { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    [NotMapped]
    public bool IsApproved => Status == "approved";
}
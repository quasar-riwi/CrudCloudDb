using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CrudCloud.api.Data.Entities;
using System.Collections.Generic;

namespace CrudCloud.api.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Apellido { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Correo { get; set; } = string.Empty;

    [Required]
    public string Contraseña { get; set; } = string.Empty; // Quitamos el StringLength para contraseñas hasheadas

    [NotMapped]
    public string? ConfirmarContraseña { get; set; } // No es obligatorio en todos los escenarios

    [Required]
    public string Plan { get; set; } = "gratis";
    
    public bool IsActive { get; set; } = false; 
    public bool EmailVerified { get; set; } = false;
    
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpires { get; set; }
    
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpires { get; set; }

    // --- Navigation properties (CORREGIDAS) ---
    
    public virtual ICollection<DatabaseInstance> Instancias { get; set; } = new List<DatabaseInstance>();
    
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    
    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
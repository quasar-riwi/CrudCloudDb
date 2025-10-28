using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CrudCloud.api.Models;

public class User : IdentityUser
{
    // Relación con el Plan del usuario
    public int PlanId { get; set; }
    public virtual Plan Plan { get; set; }

    // ID del cliente en la pasarela de pagos para gestionar suscripciones
    [MaxLength(100)]
    public string? MercadoPagoCustomerId { get; set; }

    // Fecha de creación, útil para auditoría y métricas
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Propiedades de navegación para acceder a los datos relacionados
    public virtual ICollection<DatabaseInstance> DatabaseInstances { get; set; } = new List<DatabaseInstance>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
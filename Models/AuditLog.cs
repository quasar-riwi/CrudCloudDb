using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrudCloud.api.Models;

public class AuditLog
{
    public long Id { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Quién realizó la acción (puede ser nulo si es una acción del sistema)
    public string? UserId { get; set; }
    public virtual User? User { get; set; }

    [Required]
    [MaxLength(100)]
    public string ActionType { get; set; } // Ej: "CreateDatabase", "UserLoginFailure"

    [MaxLength(50)]
    public string? EntityType { get; set; } // Ej: "DatabaseInstance"

    [MaxLength(100)]
    public string? EntityId { get; set; } // Ej: "123"

    // Guardará detalles como JSON. En PostgreSQL se puede mapear a un tipo `jsonb`.
    [Column(TypeName = "jsonb")]
    public string Details { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } // "Success", "Failure"

    [MaxLength(45)]
    public string? IpAddress { get; set; }
}
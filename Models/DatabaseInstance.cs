using System.ComponentModel.DataAnnotations;

namespace CrudCloud.api.Models;

public class DatabaseInstance
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } // Nombre único de la instancia dado por el usuario

    [Required]
    public string UserId { get; set; } // Llave foránea al dueño
    public virtual User User { get; set; }

    public int EngineId { get; set; } // Llave foránea al motor
    public virtual DatabaseEngine Engine { get; set; }

    [Required]
    [MaxLength(63)] // Límite de nombre de DB en PostgreSQL
    public string DbName { get; set; }

    [Required]
    [MaxLength(63)] // Límite de nombre de usuario en PostgreSQL
    public string DbUser { get; set; }

    [Required]
    public string DbPasswordHash { get; set; } // Guardar SIEMPRE el hash, nunca la contraseña

    public int Port { get; set; } // Puerto externo mapeado en el host

    [Required]
    [MaxLength(100)]
    public string Host { get; set; } // IP o dominio del servidor donde corre el contenedor

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } // "Creating", "Active", "Deleting", "Error"

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Para el borrado lógico (soft delete). Si es null, la instancia está activa.
    public DateTime? DeletedAt { get; set; }
}
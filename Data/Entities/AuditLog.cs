using System.ComponentModel.DataAnnotations;

namespace CrudCloud.api.Models;

public class AuditLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int? UsuarioId { get; set; }

    [Required]
    public string Accion { get; set; } = string.Empty; // "Create", "Delete", etc.

    [Required]
    public string Entidad { get; set; } = string.Empty; // "DatabaseInstance"

    public string? Detalle { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}
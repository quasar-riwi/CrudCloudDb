using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    [StringLength(100, MinimumLength = 6)]
    public string Contraseña { get; set; } = string.Empty;

    [NotMapped] // No se guarda en BD, solo para validación
    [Compare("Contraseña", ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmarContraseña { get; set; } = string.Empty;

    [Required]
    public string Plan { get; set; } = "Gratis"; // Gratis, Intermedio, Avanzado

    public ICollection<DatabaseInstance> Instancias { get; set; } = new List<DatabaseInstance>();
}
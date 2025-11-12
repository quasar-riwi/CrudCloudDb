using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CrudCloud.api.Models;

// Asegúrate de que el namespace coincida con la ubicación del archivo.
// Si lo mueves a Data/Entities, debería ser CrudCloud.api.Data.Entities
namespace CrudCloud.api.Data.Entities; 

[Table("DatabaseInstances")] // Es una buena práctica nombrar las tablas en plural
public class DatabaseInstance
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)] // Buena práctica: definir un límite de longitud
    public string Motor { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;
    
    // --- ESTA ES LA SECCIÓN CORREGIDA ---
    
    /// <summary>
    /// La clave externa que apunta a la tabla de Usuarios.
    /// </summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// La propiedad de navegación hacia el Usuario dueño de esta instancia.
    /// El atributo ForeignKey aquí le dice a EF que 'UserId' es la clave externa.
    /// </summary>
    [ForeignKey("UserId")]
    public virtual User User { get; set; }

    // --- FIN DE LA SECCIÓN CORREGIDA ---

    [Required]
    [MaxLength(100)]
    public string UsuarioDb { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)] // No guardar contraseñas en texto plano, pero si lo haces, dale espacio
    public string Contraseña { get; set; } = string.Empty;

    public int Puerto { get; set; }

    [Required]
    [MaxLength(255)]
    public string Host { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Estado { get; set; } = "running";

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
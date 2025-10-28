using System.ComponentModel.DataAnnotations;

namespace CrudCloud.api.Models;

public class DatabaseEngine
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } // "MySQL", "PostgreSQL", "MongoDB", etc.

    [Required]
    [MaxLength(100)]
    public string DockerImage { get; set; }

    // Puerto por defecto que usa internamente el motor
    public int DefaultPort { get; set; }
}
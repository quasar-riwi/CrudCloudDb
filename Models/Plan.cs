using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrudCloud.api.Models;

public class Plan
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } // "Gratuito", "Intermedio", "Avanzado"

    // Límite de bases de datos por cada motor
    public int MaxDatabasesPerEngine { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Price { get; set; } // Precio en COP

    // ID del plan correspondiente en la pasarela de pagos
    [MaxLength(100)]
    public string? MercadoPagoPlanId { get; set; }
}
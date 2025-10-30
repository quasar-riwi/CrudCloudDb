using System.ComponentModel.DataAnnotations;

namespace CrudCloud.api.DTOs;

public class UserUpdateDto
{
    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    public string Apellido { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Correo { get; set; } = string.Empty;

    public string Plan { get; set; } = "Gratis";
}
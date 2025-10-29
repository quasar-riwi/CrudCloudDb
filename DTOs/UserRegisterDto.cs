using System.ComponentModel.DataAnnotations;

namespace CrudCloud.api.DTOs;

public class UserRegisterDto
{
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

    [Required]
    [Compare("Contraseña", ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmarContraseña { get; set; } = string.Empty;

    public string Plan { get; set; } = "Gratis"; // Gratis, Intermedio, Avanzado
}
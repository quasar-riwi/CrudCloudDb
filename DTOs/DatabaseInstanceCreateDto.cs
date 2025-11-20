using System.ComponentModel.DataAnnotations;

namespace CrudCloud.api.DTOs;

public class DatabaseInstanceCreateDto
{
    [Required]
    public string Motor { get; set; } = string.Empty;
}
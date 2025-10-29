﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrudCloud.api.Models;

public class DatabaseInstance
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Motor { get; set; } = string.Empty; // MySQL, PostgreSQL, etc.

    [Required]
    public string Nombre { get; set; } = string.Empty;
    
    [ForeignKey("User")]
    [Required]
    public int UsuarioId { get; set; }
    public User? User { get; set; }

    [Required]
    public string UsuarioDb { get; set; } = string.Empty;

    [Required]
    public string Contraseña { get; set; } = string.Empty;

    public int Puerto { get; set; }

    [Required]
    public string Estado { get; set; } = "running"; // running | stopped | deleted

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
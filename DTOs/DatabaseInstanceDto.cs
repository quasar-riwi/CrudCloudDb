namespace CrudCloud.api.DTOs;

public class DatabaseInstanceDto
{
    public int Id { get; set; }
    public string Motor { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string UsuarioDb { get; set; } = string.Empty;
    public string Contraseña { get; set; } = string.Empty;
    public int Puerto { get; set; }
    public string Estado { get; set; } = string.Empty;
}
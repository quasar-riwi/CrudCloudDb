namespace CrudCloud.api.DTOs
{
    public class UserDetailDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty; // Valor inicial
        public string Apellido { get; set; } = string.Empty; // Valor inicial
        public string Correo { get; set; } = string.Empty; // Valor inicial
        public string Plan { get; set; } = string.Empty; // Valor inicial
        public bool IsActive { get; set; }
        public List<DatabaseInstanceDto> Instancias { get; set; } = new List<DatabaseInstanceDto>(); // Valor inicial
    }
}
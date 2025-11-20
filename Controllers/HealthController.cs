using CrudCloud.api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Npgsql;
using MongoDB.Driver;
using System.Net.Sockets;

namespace CrudCloud.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HealthController(AppDbContext context)
        {
            _context = context;
        }

        // 🔹 Endpoint general: valida cada instancia
        [HttpGet("instances")]
        public async Task<IActionResult> CheckAllInstances()
        {
            var instances = await _context.DatabaseInstances.ToListAsync();
            var results = new List<object>();

            foreach (var i in instances)
            {
                bool isHealthy = false;
                string message = "";
                string connectionString = BuildConnectionString(i);

                try
                {
                    switch (i.Motor.ToLower())
                    {
                        case "mysql":
                            using (var conn = new MySqlConnection(connectionString))
                            {
                                await conn.OpenAsync();
                                isHealthy = conn.State == System.Data.ConnectionState.Open;
                            }
                            break;

                        case "postgresql":
                            using (var conn = new NpgsqlConnection(connectionString))
                            {
                                await conn.OpenAsync();
                                isHealthy = conn.State == System.Data.ConnectionState.Open;
                            }
                            break;

                        case "sqlserver":
                        case "sql server":
                            using (var conn = new SqlConnection(connectionString))
                            {
                                await conn.OpenAsync();
                                isHealthy = conn.State == System.Data.ConnectionState.Open;
                            }
                            break;

                        case "mongodb":
                            var client = new MongoClient(connectionString);
                            var dbList = await client.ListDatabaseNamesAsync();
                            isHealthy = dbList != null;
                            break;

                        default:
                            message = "Motor no reconocido.";
                            break;
                    }

                    if (isHealthy)
                        message = "Conexión exitosa.";
                    else if (string.IsNullOrEmpty(message))
                        message = "No se pudo establecer la conexión.";
                }
                catch (SocketException ex)
                {
                    message = $"Error de red: {ex.Message}";
                }
                catch (Exception ex)
                {
                    message = $"Error: {ex.Message}";
                }

                results.Add(new
                {
                    i.Id,
                    i.Nombre,
                    i.Motor,
                    Estado = isHealthy ? "running" : "down",
                    Mensaje = message,
                    i.FechaCreacion
                });
            }

            var response = new
            {
                StatusGlobal = results.Any(r => ((string)((dynamic)r).Estado) == "down") ? "issues" : "healthy",
                CheckedAt = DateTime.UtcNow,
                Total = results.Count,
                Instances = results
            };

            return Ok(response);
        }

        // 🔸 Método auxiliar: construir cadena de conexión según motor
        private static string BuildConnectionString(dynamic instance)
        {
            return instance.Motor.ToLower() switch
            {
                "mysql" => $"Server={instance.Host};Port={instance.Puerto};Database={instance.Nombre};User={instance.UsuarioDb};Password={instance.Contraseña};SslMode=Preferred;",
                "postgresql" => $"Host={instance.Host};Port={instance.Puerto};Database={instance.Nombre};Username={instance.UsuarioDb};Password={instance.Contraseña};",
                "sqlserver" or "sql server" => $"Server={instance.Host},{instance.Puerto};Database={instance.Nombre};User Id={instance.UsuarioDb};Password={instance.Contraseña};TrustServerCertificate=True;",
                "mongodb" => $"mongodb://{instance.UsuarioDb}:{instance.Contraseña}@{instance.Host}:{instance.Puerto}/{instance.Nombre}",
                _ => ""
            };
        }
    }
}

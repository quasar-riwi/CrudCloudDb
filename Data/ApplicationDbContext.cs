using CrudCloud.api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CrudCloud.api.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // --- Tablas de tu aplicación ---
    public DbSet<Plan> Plans { get; set; }
    public DbSet<DatabaseEngine> DatabaseEngines { get; set; }
    public DbSet<DatabaseInstance> DatabaseInstances { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }


    // --- CONFIGURACIÓN ADICIONAL (LA PARTE QUE FALTABA) ---
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Es MUY importante llamar a base.OnModelCreating(builder) primero.
        // Esto configura todo lo relacionado con ASP.NET Core Identity (usuarios, roles, etc.).
        base.OnModelCreating(builder);

        // Configuración para la entidad AuditLog
        builder.Entity<AuditLog>()
            .Property(p => p.Details)
            // Esto le dice a EF Core que use el tipo de columna 'jsonb' de PostgreSQL,
            // que es optimizado para almacenar y consultar datos JSON.
            .HasColumnType("jsonb");

        // Configuración para la entidad DatabaseInstance
        builder.Entity<DatabaseInstance>()
            .Property(p => p.Status)
            // Esto convierte el enum 'DatabaseStatus' a un string (ej: "Active") en la base de datos.
            // Es mucho más legible que guardar un número (0, 1, 2...).
            .HasConversion<string>();
    }
}
using CrudCloud.api.Models;
using Microsoft.EntityFrameworkCore;

namespace CrudCloud.api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<DatabaseInstance> DatabaseInstances { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<User> Users { get; set; }
}
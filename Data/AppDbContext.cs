using CrudCloud.api.Models;
using CrudCloud.api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrudCloud.api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Users first (main entity)
    public DbSet<User> Users { get; set; }
    
    // Related to users
    public DbSet<DatabaseInstance> DatabaseInstances { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    
    // Payment system (nuevos)
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
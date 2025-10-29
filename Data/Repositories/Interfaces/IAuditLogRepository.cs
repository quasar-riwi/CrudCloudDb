using CrudCloud.api.Models;

namespace CrudCloud.api.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log);
    Task<IEnumerable<AuditLog>> GetAllAsync();
    Task SaveChangesAsync();
}
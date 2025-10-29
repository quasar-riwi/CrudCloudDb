using CrudCloud.api.Models;

namespace CrudCloud.api.Services;

public interface IAuditService
{
    Task LogAsync(int userId, string action, string entity, string? detail = null);
    Task<IEnumerable<AuditLog>> GetAllAsync();
    Task<IEnumerable<AuditLog>> GetByUserAsync(int userId);
}
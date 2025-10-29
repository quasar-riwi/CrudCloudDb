using CrudCloud.api.Models;
using CrudCloud.api.Repositories;

namespace CrudCloud.api.Services;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _repo;

    public AuditService(IAuditLogRepository repo)
    {
        _repo = repo;
    }

    public async Task LogAsync(int userId, string action, string entity, string? detail = null)
    {
        var log = new AuditLog
        {
            UsuarioId = userId,
            Accion = action,
            Entidad = entity,
            Detalle = detail
        };

        await _repo.AddAsync(log);
        await _repo.SaveChangesAsync();
    }
    
    public async Task<IEnumerable<AuditLog>> GetAllAsync()
    {
        return await _repo.GetAllAsync();
    }
    
    public async Task<IEnumerable<AuditLog>> GetByUserAsync(int userId)
    {
        var allLogs = await _repo.GetAllAsync();
        return allLogs
            .Where(log => log.UsuarioId == userId)
            .OrderByDescending(log => log.Fecha);
    }
}

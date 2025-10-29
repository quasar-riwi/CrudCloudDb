using CrudCloud.api.Models;

namespace CrudCloud.api.Repositories;

public interface IDatabaseInstanceRepository
{
    Task<IEnumerable<DatabaseInstance>> GetByUserAsync(int userId);
    Task<int> CountByUserAndMotorAsync(int userId, string motor);
    Task<DatabaseInstance?> GetByIdAsync(int id);
    Task AddAsync(DatabaseInstance instance);
    Task DeleteAsync(DatabaseInstance instance);
    Task SaveChangesAsync();
}
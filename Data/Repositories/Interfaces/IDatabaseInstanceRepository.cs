using CrudCloud.api.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CrudCloud.api.Repositories; // Asegúrate de que este sea el namespace correcto

public interface IDatabaseInstanceRepository
{
    Task<IEnumerable<DatabaseInstance>> GetByUserAsync(int userId);
    Task<int> CountByUserAndMotorAsync(int userId, string motor);
    Task<DatabaseInstance?> GetByIdAsync(int id);
    Task AddAsync(DatabaseInstance instance);
    void Delete(DatabaseInstance instance); // ✅ CORRECCIÓN: Cambiado a void síncrono
    Task SaveChangesAsync();
}
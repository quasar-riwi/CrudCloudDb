using CrudCloud.api.Data;
using CrudCloud.api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrudCloud.api.Repositories;

namespace CrudCloud.api.Data.Repositories.Implementations; // Asegúrate de que este sea el namespace correcto

public class DatabaseInstanceRepository : IDatabaseInstanceRepository
{
    private readonly AppDbContext _context;
    
    public DatabaseInstanceRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<IEnumerable<DatabaseInstance>> GetByUserAsync(int userId)
    {
        // ✅ CORRECCIÓN: Se cambió 'x.UsuarioId' a 'x.UserId'
        return await _context.DatabaseInstances.Where(x => x.UserId == userId).ToListAsync();
    }

    public async Task<int> CountByUserAndMotorAsync(int userId, string motor)
    {
        // ✅ CORRECCIÓN: Se cambió 'x.UsuarioId' a 'x.UserId'
        return await _context.DatabaseInstances.CountAsync(x => x.UserId == userId && x.Motor == motor);
    }

    public async Task<DatabaseInstance?> GetByIdAsync(int id)
    {
        return await _context.DatabaseInstances.FindAsync(id);
    }

    public async Task AddAsync(DatabaseInstance instance)
    {
        // ✅ MEJORA: AddAsync es para casos especiales. Add es la forma estándar y correcta aquí.
        // Se envuelve en un Task.CompletedTask para cumplir con la interfaz asíncrona.
        _context.DatabaseInstances.Add(instance);
        await Task.CompletedTask;
    }

    public void Delete(DatabaseInstance instance)
    {
        // ✅ CORRECCIÓN: Implementa el método síncrono 'Delete'
        _context.DatabaseInstances.Remove(instance);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
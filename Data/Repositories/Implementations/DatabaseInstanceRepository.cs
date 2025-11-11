using CrudCloud.api.Data;
using CrudCloud.api.Models;
using Microsoft.EntityFrameworkCore;

namespace CrudCloud.api.Repositories;

public class DatabaseInstanceRepository : IDatabaseInstanceRepository
{
    private readonly AppDbContext _context;
    
    public DatabaseInstanceRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<IEnumerable<DatabaseInstance>> GetByUserAsync(int userId)
        => await _context.DatabaseInstances.Where(x => x.UserId == userId).ToListAsync();

    public async Task<int> CountByUserAndMotorAsync(int userId, string motor)
        => await _context.DatabaseInstances.CountAsync(x => x.UserId == userId && x.Motor == motor);

    public async Task<DatabaseInstance?> GetByIdAsync(int id)
        => await _context.DatabaseInstances.FindAsync(id);

    public async Task AddAsync(DatabaseInstance instance)
        => await _context.DatabaseInstances.AddAsync(instance);

    public async Task DeleteAsync(DatabaseInstance instance)
        => _context.DatabaseInstances.Remove(instance);

    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();
}
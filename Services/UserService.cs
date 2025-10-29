using CrudCloud.api.Data;
using CrudCloud.api.DTOs;
using CrudCloud.api.Models;
using Microsoft.EntityFrameworkCore;

namespace CrudCloud.api.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User> RegisterAsync(UserRegisterDto dto)
    {
        // Validar correo único
        if (await _context.Users.AnyAsync(u => u.Correo == dto.Correo))
            throw new InvalidOperationException("El correo ya está registrado.");

        var user = new User
        {
            Nombre = dto.Nombre,
            Apellido = dto.Apellido,
            Correo = dto.Correo,
            Contraseña = dto.Contraseña, // (por ahora sin hash)
            Plan = dto.Plan
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User?> UpdateAsync(int id, UserUpdateDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null;

        user.Nombre = dto.Nombre;
        user.Apellido = dto.Apellido;
        user.Correo = dto.Correo;
        user.Plan = dto.Plan;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.Include(u => u.Instancias).FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users.ToListAsync();
    }
}
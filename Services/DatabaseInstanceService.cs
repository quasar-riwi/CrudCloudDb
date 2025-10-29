using CrudCloud.api.Models;
using AutoMapper;
using CrudCloud.api.Data;
using CrudCloud.api.DTOs;
using CrudCloud.api.Repositories;
using CrudCloud.api.Utils;
using Microsoft.EntityFrameworkCore;

namespace CrudCloud.api.Services;

public class DatabaseInstanceService : IDatabaseInstanceService
{
    private readonly IDatabaseInstanceRepository _repo;
    private readonly IAuditService _audit;
    private readonly IMapper _mapper;
    private readonly AppDbContext _context;

    public DatabaseInstanceService(IDatabaseInstanceRepository repo, IAuditService audit, IMapper mapper, AppDbContext context)
    {
        _repo = repo;
        _audit = audit;
        _mapper = mapper;
        _context = context;
    }

    public async Task<IEnumerable<DatabaseInstanceDto>> GetUserInstancesAsync(int userId)
    {
        var list = await _repo.GetByUserAsync(userId);
        return _mapper.Map<IEnumerable<DatabaseInstanceDto>>(list);
    }

    public async Task<DatabaseInstanceDto> CreateInstanceAsync(int userId, DatabaseInstanceCreateDto dto)
    {
        // 1️⃣ Validar motor permitido
        if (!PlanLimits.MotoresPermitidos.Any(m => m.Equals(dto.Motor, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"Motor no permitido: {dto.Motor}");

        // 2️⃣ Obtener usuario y su plan
        var user = await _context.Users
            .Include(u => u.Instancias)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new Exception("Usuario no encontrado.");

        var limite = PlanLimits.MaxPerMotor[user.Plan];

        // 3️⃣ Validar límite por plan y motor
        int cantidadActual = user.Instancias.Count(i => i.Motor == dto.Motor);
        if (cantidadActual >= limite)
            throw new Exception($"Límite de {limite} bases de datos alcanzado para el plan {user.Plan}.");

        // 4️⃣ Generar credenciales únicas
        var random = Guid.NewGuid().ToString("N").Substring(0, 6);
        var instance = new DatabaseInstance
        {
            UsuarioId = userId,
            Motor = dto.Motor,
            Nombre = $"bd_{random}",
            UsuarioDb = $"usr_{random}",
            Contraseña = $"pwd_{random}",
            Puerto = GenerarPuerto(dto.Motor),
            Estado = "running"
        };

        await _repo.AddAsync(instance);
        await _repo.SaveChangesAsync();

        await _audit.LogAsync(userId, "Create", "DatabaseInstance", $"Creada {dto.Motor}: {instance.Nombre}");

        return _mapper.Map<DatabaseInstanceDto>(instance);
    }

    public async Task<bool> DeleteInstanceAsync(int userId, int id)
    {
        var instance = await _repo.GetByIdAsync(id);
        if (instance == null || instance.UsuarioId != userId)
            return false;

        await _repo.DeleteAsync(instance);
        await _repo.SaveChangesAsync();

        await _audit.LogAsync(userId, "Delete", "DatabaseInstance", $"Eliminada {instance.Nombre}");
        return true;
    }

    private int GenerarPuerto(string motor) => motor switch
    {
        "PostgreSQL" => 5432 + new Random().Next(1, 1000),
        "MySQL" => 3306 + new Random().Next(1, 1000),
        "MongoDB" => 27017 + new Random().Next(1, 1000),
        "SQLServer" => 1433 + new Random().Next(1, 1000),
        "Redis" => 6379 + new Random().Next(1, 1000),
        "Cassandra" => 9042 + new Random().Next(1, 1000),
        _ => 5000 + new Random().Next(1, 1000)
    };
}
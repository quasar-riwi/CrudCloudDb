using CrudCloud.api.Models;
using AutoMapper;
using CrudCloud.api.Data;
using CrudCloud.api.DTOs;
using CrudCloud.api.Repositories;
using CrudCloud.api.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CrudCloud.api.Services;

public class DatabaseInstanceService : IDatabaseInstanceService
{
    private readonly IDatabaseInstanceRepository _repo;
    private readonly IAuditService _audit;
    private readonly IMapper _mapper;
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public DatabaseInstanceService(
        IDatabaseInstanceRepository repo,
        IAuditService audit,
        IMapper mapper,
        AppDbContext context,
        IConfiguration config)
    {
        _repo = repo;
        _audit = audit;
        _mapper = mapper;
        _context = context;
        _config = config;
    }

    public async Task<IEnumerable<DatabaseInstanceDto>> GetUserInstancesAsync(int userId)
    {
        var list = await _repo.GetByUserAsync(userId);
        return _mapper.Map<IEnumerable<DatabaseInstanceDto>>(list);
    }

    public async Task<DatabaseInstanceDto> CreateInstanceAsync(int userId, DatabaseInstanceCreateDto dto)
    {
        // 1️⃣ Validar motor permitido (case-insensitive)
        if (!PlanLimits.MotoresPermitidos.Any(m => m.Equals(dto.Motor, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"Motor no permitido: {dto.Motor}");

        // 2️⃣ Obtener usuario y su plan
        var user = await _context.Users
            .Include(u => u.Instancias)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new Exception("Usuario no encontrado.");

        var limite = PlanLimits.MaxPerMotor.ContainsKey(user.Plan) ? PlanLimits.MaxPerMotor[user.Plan] : PlanLimits.MaxPerMotor["Gratis"];

        // 3️⃣ Validar límite por plan y motor
        int cantidadActual = user.Instancias.Count(i => i.Motor.Equals(dto.Motor, StringComparison.OrdinalIgnoreCase));
        if (cantidadActual >= limite)
            throw new Exception($"Límite de {limite} bases de datos alcanzado para el plan {user.Plan}.");

        // 4️⃣ Generar credenciales únicas (sanitizadas)
        var random = Guid.NewGuid().ToString("N").Substring(0, 6);
        var rawName = $"bd_{random}";
        var rawUser = $"usr_{random}";
        var rawPwd = $"pwd_{Guid.NewGuid().ToString("N").Substring(0, 12)}";

        var name = IdentifierSanitizer.Sanitize(rawName);
        var dbUser = IdentifierSanitizer.Sanitize(rawUser);
        var password = rawPwd; // password can include more chars

        // puerto según motor
        var puerto = GenerarPuerto(dto.Motor);

        // 5️⃣ Intentar crear la base real (si falla, no persisto en app DB)
        try
        {
            await DatabaseCreator.CrearInstanciaRealAsync(
                dto.Motor,
                name,
                dbUser,
                password,
                puerto,
                _config);
        }
        catch (Exception ex)
        {
            // registra auditoría de fallo
            await _audit.LogAsync(userId, "CreateFailed", "DatabaseInstance", $"Error creando {dto.Motor}:{name} -> {ex.Message}");
            throw new Exception($"Fallo al crear la instancia física: {ex.Message}", ex);
        }

        // 6️⃣ Crear entidad local y persistir
        var instance = new DatabaseInstance
        {
            UsuarioId = userId,
            Motor = dto.Motor,
            Nombre = name,
            UsuarioDb = dbUser,
            Contraseña = password,
            Puerto = puerto,
            Estado = "running"
        };

        await _repo.AddAsync(instance);
        await _repo.SaveChangesAsync();

        await _audit.LogAsync(userId, "Create", "DatabaseInstance", $"Creada {dto.Motor}: {instance.Nombre}");

        var dtoOut = _mapper.Map<DatabaseInstanceDto>(instance);
        // incluir credenciales en respuesta (solo primera vez)
        dtoOut.UsuarioDb = dbUser;
        dtoOut.Contraseña = password;
        dtoOut.Puerto = puerto;

        return dtoOut;
    }

    public async Task<bool> DeleteInstanceAsync(int userId, int id)
    {
        var instance = await _repo.GetByIdAsync(id);
        if (instance == null || instance.UsuarioId != userId)
            return false;

        // 1) intentar eliminar en motor real (si aplica)
        try
        {
            await DatabaseCreator.EliminarInstanciaRealAsync(instance.Motor, instance.Nombre, instance.UsuarioDb, _config);
        }
        catch (Exception ex)
        {
            // log y proceed to delete local (o decidir no eliminar)
            await _audit.LogAsync(userId, "DeleteFailed", "DatabaseInstance", $"Error eliminando {instance.Motor}:{instance.Nombre} -> {ex.Message}");
            throw;
        }

        // 2) eliminar registro local
        await _repo.DeleteAsync(instance);
        await _repo.SaveChangesAsync();

        await _audit.LogAsync(userId, "Delete", "DatabaseInstance", $"Eliminada {instance.Nombre}");
        return true;
    }

    private int GenerarPuerto(string motor) => motor.ToLower() switch
    {
        "postgresql" => 5432 + new Random().Next(1, 1000),
        "mysql" => 3306 + new Random().Next(1, 1000),
        "mongodb" => 27017 + new Random().Next(1, 1000),
        "sqlserver" => 1433 + new Random().Next(1, 1000),
        "redis" => 6379 + new Random().Next(1, 1000),
        "cassandra" => 9042 + new Random().Next(1, 1000),
        _ => 5000 + new Random().Next(1, 1000)
    };
}

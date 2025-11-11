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
    private readonly IEmailService _emailService;

    public DatabaseInstanceService(
        IDatabaseInstanceRepository repo,
        IAuditService audit,
        IMapper mapper,
        AppDbContext context,
        IConfiguration config,
        IEmailService emailService)
    {
        _repo = repo;
        _audit = audit;
        _mapper = mapper;
        _context = context;
        _config = config;
        _emailService = emailService;
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

        var limite = PlanLimits.MaxPerMotor.ContainsKey(user.Plan)
            ? PlanLimits.MaxPerMotor[user.Plan]
            : PlanLimits.MaxPerMotor["Gratis"];

        // 3️⃣ Validar límite por plan y motor
        int cantidadActual = user.Instancias.Count(i => i.Motor.Equals(dto.Motor, StringComparison.OrdinalIgnoreCase));
        if (cantidadActual >= limite)
            throw new Exception($"Límite de {limite} bases de datos alcanzado para el plan {user.Plan}.");

        // 4️⃣ Crear nombres únicos y coherentes
        var motorLower = dto.Motor.ToLower();
        var sufijo = Guid.NewGuid().ToString("N").Substring(0, 6);
        var nombre = $"db_{userId}_{motorLower}_{sufijo}";
        var usuarioDb = $"usr_{userId}_{motorLower}_{sufijo}";
        var contraseña = $"Pwd_{motorLower.Substring(0, 2).ToUpper()}_{sufijo}_{new Random().Next(10, 99)}!";
        var puerto = ObtenerPuertoPorMotor(motorLower);
        var host = ObtenerHostPorMotor(motorLower, _config);

        // 5️⃣ Crear instancia real
        try
        {
            await DatabaseCreator.CrearInstanciaRealAsync(motorLower, nombre, usuarioDb, contraseña, puerto, _config);
        }
        catch (Exception ex)
        {
            await _audit.LogAsync(userId, "CreateFailed", "DatabaseInstance", $"Error creando {dto.Motor}:{nombre} -> {ex.Message}");
            throw new Exception($"Fallo al crear la instancia física: {ex.Message}", ex);
        }

        // 6️⃣ Crear entidad local
        var instance = new DatabaseInstance
        {
            UsuarioId = userId,
            Motor = dto.Motor,
            Nombre = nombre,
            UsuarioDb = usuarioDb,
            Contraseña = contraseña,
            Puerto = puerto,
            Host = host,
            Estado = "running",
            FechaCreacion = DateTime.UtcNow
        };

        await _repo.AddAsync(instance);
        await _repo.SaveChangesAsync();

        await _audit.LogAsync(userId, "Create", "DatabaseInstance", $"Creada {dto.Motor}: {instance.Nombre}");
        
        try
        {
            // --- ✅ CORREGIDO: Se usa user.Correo en lugar de user.Email ---
            await _emailService.SendDatabaseCreatedEmailAsync(user.Correo, user.Nombre, instance);
        }
        catch (Exception ex)
        {
            await _audit.LogAsync(userId, "EmailFailed", "DatabaseInstance", $"Fallo al enviar correo para {instance.Nombre}: {ex.Message}");
        }

        var dtoOut = _mapper.Map<DatabaseInstanceDto>(instance);
        dtoOut.UsuarioDb = usuarioDb;
        dtoOut.Contraseña = contraseña;
        dtoOut.Puerto = puerto;
        dtoOut.Host = host;

        return dtoOut;
    }

    public async Task<bool> DeleteInstanceAsync(int userId, int id)
    {
        // --- ✅ CORREGIDO: Se usa _context.DatabaseInstances en lugar de _context.Instances ---
        var instance = await _context.DatabaseInstances
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (instance == null || instance.UsuarioId != userId)
            return false;
        
        // --- ✅ CORREGIDO: Se usa instance.User.Correo en lugar de instance.User.Email ---
        var userEmail = instance.User.Correo;
        var userName = instance.User.Nombre;

        try
        {
            await DatabaseCreator.EliminarInstanciaRealAsync(instance.Motor, instance.Nombre, instance.UsuarioDb, _config);
        }
        catch (Exception ex)
        {
            await _audit.LogAsync(userId, "DeleteFailed", "DatabaseInstance", $"Error eliminando {instance.Motor}:{instance.Nombre} -> {ex.Message}");
            throw;
        }

        await _repo.DeleteAsync(instance);
        await _repo.SaveChangesAsync();

        await _audit.LogAsync(userId, "Delete", "DatabaseInstance", $"Eliminada {instance.Nombre}");
        
        try
        {
            await _emailService.SendDatabaseDeletedEmailAsync(userEmail, userName, instance);
        }
        catch (Exception ex)
        {
            await _audit.LogAsync(userId, "EmailFailed", "DatabaseInstance", $"Fallo al enviar correo de eliminación para {instance.Nombre}: {ex.Message}");
        }

        return true;
    }

    private static int ObtenerPuertoPorMotor(string motor) => motor switch
    {
        "postgresql" => 5432,
        "mysql" => 3307,
        "mongodb" => 27017,
        "sqlserver" => 1433,
        _ => 5000
    };

    private static string ObtenerHostPorMotor(string motor, IConfiguration config)
    {
        return motor switch
        {
            "postgresql" => config["Hosts:Postgres"] ?? "88.198.127.218",
            "mysql" => config["Hosts:MySQL"] ?? "88.198.127.218",
            "mongodb" => config["Hosts:Mongo"] ?? "88.198.127.218",
            "sqlserver" => config["Hosts:SqlServer"] ?? "88.198.127.218",
            _ => "88.198.127.218"
        };
    }
}
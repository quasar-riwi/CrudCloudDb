using CrudCloud.api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrudCloud.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _context;

    public HealthController(AppDbContext context)
    {
        _context = context;
    }

    // 🔹 Endpoint 1: Health general (todas las instancias)
    [HttpGet("instances")]
    public async Task<IActionResult> CheckAllInstances()
    {
        var instances = await _context.DatabaseInstances
            .Select(i => new
            {
                i.Id,
                i.Nombre,
                i.Motor,
                i.Estado,
                i.FechaCreacion
            })
            .ToListAsync();

        var response = new
        {
            Status = "Healthy",
            CheckedAt = DateTime.UtcNow,
            Total = instances.Count,
            Instances = instances
        };

        return Ok(response);
    }

    // 🔹 Endpoint 2: Health por motor (PostgreSQL, MySQL, etc.)
    [HttpGet("{motor}")]
    public async Task<IActionResult> CheckByMotor(string motor)
    {
        var instances = await _context.DatabaseInstances
            .Where(i => i.Motor.ToLower() == motor.ToLower())
            .Select(i => new
            {
                i.Id,
                i.Nombre,
                i.Motor,
                i.Estado,
                i.FechaCreacion
            })
            .ToListAsync();

        if (!instances.Any())
            return NotFound(new { Message = $"No hay instancias activas para {motor}." });

        var response = new
        {
            Motor = motor,
            EstadoGlobal = instances.Any(i => i.Estado == "running") ? "running" : "stopped",
            Revisadas = instances.Count,
            Instancias = instances
        };

        return Ok(response);
    }
}